using System;

namespace Crt
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                return Commands.Run(args);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine("error: " + ex.Message);
                return 2;
            }
            catch (DllNotFoundException ex)
            {
                Console.Error.WriteLine("error: " + ex.Message);
                Console.Error.WriteLine("ControlLib.dll was not found. This tool requires Intel graphics drivers with IGCL support.");
                return 3;
            }
            catch (BadImageFormatException ex)
            {
                Console.Error.WriteLine("error: " + ex.Message);
                Console.Error.WriteLine("ControlLib.dll is not usable by this process (architecture mismatch).");
                return 3;
            }
            catch (IgclException ex)
            {
                Console.Error.WriteLine("error: " + ex.Message);
                if (ex.Result == IgclResult.InsufficientPermissions)
                {
                    Console.Error.WriteLine("hint: run this command from an elevated terminal.");
                }

                return 3;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("error: " + ex.Message);
                return 1;
            }
        }
    }
}
