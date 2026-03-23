using System;
using System.Collections.Generic;
using KBMS.Storage.V3;
using Tuple = KBMS.Storage.V3.Tuple;

namespace KBMS.Knowledge.V3.Execution;

/// <summary>
/// Execution node that scans sequentially through all stored pages and slots for a given Concept.
/// Streams records directly from the DiskManager/BufferPool without loading everything into RAM.
/// </summary>
public class SequentialScanOperator : IExecutionOperator
{
    private readonly BufferPoolManager _bpm;
    private readonly List<int> _pageIds; // Tracks which physical pages belong to this Concept
    
    private int _currentPageIdx = 0;
    private int _currentSlotIdx = 0;
    private Page? _currentPageObj = null;
    private SlottedPage? _currentSlottedPage = null;

    public SequentialScanOperator(BufferPoolManager bpm, List<int> pageIds)
    {
        _bpm = bpm;
        _pageIds = pageIds;
    }

    public void Init()
    {
        _currentPageIdx = 0;
        _currentSlotIdx = 0;
        UnpinCurrentPage();
        LoadNextPage();
    }

    public Tuple? Next()
    {
        while (_currentSlottedPage != null)
        {
            if (_currentSlotIdx < _currentSlottedPage.TupleCount)
            {
                byte[]? rawTuple = _currentSlottedPage.GetTuple(_currentSlotIdx++);
                if (rawTuple != null) // Avoid deleted slots (tombstones)
                {
                    return Tuple.Deserialize(rawTuple);
                }
            }
            else
            {
                // Reached end of current page's tuples, move to the next physical page
                UnpinCurrentPage();
                _currentPageIdx++;
                LoadNextPage();
                _currentSlotIdx = 0;
            }
        }
        return null; // EOF
    }

    private void LoadNextPage()
    {
        if (_currentPageIdx < _pageIds.Count)
        {
            _currentPageObj = _bpm.FetchPage(_pageIds[_currentPageIdx]);
            if (_currentPageObj != null)
            {
                _currentSlottedPage = new SlottedPage(_currentPageObj);
            }
        }
        else
        {
            _currentSlottedPage = null; // No more pages
        }
    }

    private void UnpinCurrentPage()
    {
        if (_currentPageObj != null)
        {
            // Inform Cache Manager we are physically done iterating over this 16KB block
            _bpm.UnpinPage(_currentPageObj.PageId, isDirty: false);
            _currentPageObj = null;
            _currentSlottedPage = null;
        }
    }

    public void Close()
    {
        UnpinCurrentPage();
    }

    public void Dispose()
    {
        Close();
    }
}
