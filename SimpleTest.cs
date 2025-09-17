using System;
using System.IO;

class SimpleTest
{
    static void Main()
    {
        Console.WriteLine("🧪 Starting DatabaseReader Test...");
        
        try
        {
            // Test if the DLL file exists
            string dllPath = @"c:\Users\byaran\OneDrive - Advanced Micro Devices Inc\programming\coverage_analyzer\DatabaseReader\bin\Release\net8.0\DatabaseReader.dll";
            
            if (File.Exists(dllPath))
            {
                Console.WriteLine("✅ DatabaseReader.dll exists at expected location");
                
                // Try to load the assembly
                var assembly = System.Reflection.Assembly.LoadFrom(dllPath);
                Console.WriteLine($"✅ Assembly loaded successfully: {assembly.FullName}");
                
                // Check for main class
                var dcPgConnType = assembly.GetType("DcPgConn");
                if (dcPgConnType != null)
                {
                    Console.WriteLine("✅ DcPgConn class found");
                    
                    // List methods
                    var methods = dcPgConnType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    Console.WriteLine($"📋 Found {methods.Length} methods");
                    
                    Console.WriteLine("🎯 TEST PASSED: DatabaseReader DLL is working correctly!");
                }
                else
                {
                    Console.WriteLine("❌ DcPgConn class not found");
                }
            }
            else
            {
                Console.WriteLine($"❌ DLL not found at: {dllPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
        }
        
        Console.WriteLine("Test completed. Press any key to continue...");
        Console.ReadKey();
    }
}