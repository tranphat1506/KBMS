using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using KBMS.Storage.V3;
using KBMS.Models;

namespace KBMS.Tests;

public class GenHexForDocs
{
    public static void Main()
    {
        string testFile = "doc_test.kdb";
        string walFile = "doc_test.kdb.wal";
        if (File.Exists(testFile)) File.Delete(testFile);
        if (File.Exists(walFile)) File.Delete(walFile);

        // 1. Initialize Storage
        var disk = new DiskManager(testFile, "DOC_KEY_2026");
        var bpm = new BufferPoolManager(disk, 10);
        var wal = new WalManagerV3(testFile);

        // 2. Create a Concept Tuple (Metadata)
        // Concept: Employee (age: INT, experience: INT)
        var conceptTuple = new KBMS.Storage.V3.Tuple();
        conceptTuple.AddGuid(Guid.Parse("11111111-2222-3333-4444-555555555555")); // ID
        conceptTuple.AddGuid(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")); // KBID
        conceptTuple.AddString("Employee"); // Name
        // Variables: age:int, exp:int (Simplified for Hex)
        conceptTuple.AddString("age:INT|experience:INT"); 
        
        // 3. Create an Instance Tuple (Data)
        var instanceTuple = new KBMS.Storage.V3.Tuple();
        instanceTuple.AddGuid(Guid.Parse("99999999-8888-7777-6666-555555555555")); // ObjID
        instanceTuple.AddString("age|experience"); // Fields
        instanceTuple.AddString("30"); // Val1
        instanceTuple.AddString("5");  // Val2

        // 4. Write to Page 1
        int pageId = disk.AllocatePage(); // Page 1
        var page = bpm.FetchPage(pageId);
        var sp = new SlottedPage(page);
        sp.Init(pageId);
        sp.InsertTuple(instanceTuple.Serialize());
        bpm.UnpinPage(pageId, true);
        
        // 5. Generate a WAL record (Simulate update)
        var txnId = Guid.NewGuid();
        var before = new byte[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var after = new byte[10] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        wal.LogWrite(txnId, pageId, before, after);

        // 6. Print Hex Dumps for Documentation
        Console.WriteLine("=== PAGE 1 HEX DUMP (EMPLOYEE INSTANCE) ===");
        PrintHex(page.Data, 64); // Header
        Console.WriteLine("...");
        PrintHex(page.Data, Page.PAGE_SIZE - 64, 64); // Tail where data is

        Console.WriteLine("\n=== WAL LOG RECORD HEX DUMP ===");
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
            Console.Write($"{(offset + i):X8}  ");
            for (int j = 0; j < 16; j++)
            {
                if (i + j < length)
                    Console.Write($"{buffer[offset + i + j]:X2} ");
                else
                    Console.Write("   ");
            }
            Console.Write(" |");
            for (int j = 0; j < 16; j++)
            {
                if (i + j < length)
                {
                    char c = (char)buffer[offset + i + j];
                    Console.Write(char.IsControl(c) ? '.' : c);
                }
            }
            Console.WriteLine("|");
        }
    }
}
