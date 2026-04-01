using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using KBMS.Storage.V3;
using KBMS.Models;

namespace GenHex;

public class Program
{
    public static void Main()
    {
        string testFile = "doc_test.kdb";
        string walFile = testFile + ".wal";
        if (File.Exists(testFile)) File.Delete(testFile);
        if (File.Exists(walFile)) File.Delete(walFile);

        // 1. Initialize Storage
        var disk = new DiskManager(testFile, "DOC_KEY_2026");
        var bpm = new BufferPoolManager(disk, 10);
        var wal = new WalManagerV3(testFile);

        // 2. Create an Instance Tuple (Data)
        // Employee { age: 30, experience: 5 }
        var instanceTuple = new KBMS.Storage.V3.Tuple();
        instanceTuple.AddGuid(Guid.Parse("99999999-8888-7777-6666-555555555555")); // ObjID
        instanceTuple.AddString("age|experience"); // Fields
        instanceTuple.AddString("30"); // Val1
        instanceTuple.AddString("5");  // Val2

        // 3. Write to Page 1
        int pageId = disk.AllocatePage(); // Page 0 is header, Page 1 is data
        var page = bpm.FetchPage(pageId);
        var sp = new SlottedPage(page);
        sp.Init(pageId);
        var data = instanceTuple.Serialize();
        sp.InsertTuple(data);
        bpm.UnpinPage(pageId, true);
        
        // 4. Generate a WAL record (Simulate update)
        var txnId = wal.Begin();
        var before = new byte[10] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99 };
        var after = new byte[10] { 0x99, 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11, 0x00 };
        wal.LogWrite(txnId, pageId, before, after);
        wal.Commit(txnId);

        // 5. Print Hex Dumps for Documentation
        Console.WriteLine("=== PAGE 1 HEX DUMP (EMPLOYEE INSTANCE) ===");
        PrintHex(page.Data, 64); // Header
        Console.WriteLine("...");
        // Usually data is at the end of the slotted page
        PrintHex(page.Data, 64, Page.PAGE_SIZE - 64); 

        Console.WriteLine("\n=== WAL LOG RECORD HEX DUMP ===");
        wal.Dispose(); // Close file to read all bytes
        byte[] walBytes = File.ReadAllBytes(walFile);
        PrintHex(walBytes, Math.Min(walBytes.Length, 128));

        // Cleanup
        bpm.Dispose();
        disk.Dispose();
    }

    static void PrintHex(byte[] buffer, int length, int offset = 0)
    {
        for (int i = 0; i < length; i += 16)
        {
            int currentOffset = offset + i;
            if (currentOffset >= buffer.Length) break;

            Console.Write($"{currentOffset:X8}  ");
            for (int j = 0; j < 16; j++)
            {
                if (currentOffset + j < buffer.Length)
                    Console.Write($"{buffer[currentOffset + j]:X2} ");
                else
                    Console.Write("   ");
            }
            Console.Write(" |");
            for (int j = 0; j < 16; j++)
            {
                if (currentOffset + j < buffer.Length)
                {
                    char c = (char)buffer[currentOffset + j];
                    Console.Write(char.IsControl(c) ? '.' : c);
                }
            }
            Console.WriteLine("|");
        }
    }
}
