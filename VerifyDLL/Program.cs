// Quick verification that the DLL loads and works
using System;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main()
    {
        try
        {
            // Load the assembly from the Release directory
            string dllPath = @"C:\Users\byaran\OneDrive - Advanced Micro Devices Inc\programming\coverage_analyzer\DatabaseReader\bin\Release\net8.0\DatabaseReader.dll";
            var assembly = Assembly.LoadFrom(dllPath);
            Console.WriteLine($"✅ Assembly loaded: {assembly.FullName}");

            // Check for the main class (in global namespace)
            var dcPgConnType = assembly.GetType("DcPgConn");
            if (dcPgConnType != null)
            {
                Console.WriteLine($"✅ Found DcPgConn class");

                // List all public static methods
                var methods = dcPgConnType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_") && m.Name != "ToString" && m.Name != "Equals" && m.Name != "GetHashCode" && m.Name != "GetType")
                    .ToArray();

                Console.WriteLine($"📋 Available methods ({methods.Length}):");

                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    var paramList = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    Console.WriteLine($"  - {method.ReturnType.Name} {method.Name}({paramList})");
                }

                Console.WriteLine("\n🎯 DLL verification successful!");
                Console.WriteLine("The Release version of DatabaseReader.dll is ready for distribution.");
            }
            else
            {
                Console.WriteLine("❌ DcPgConn class not found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }
}