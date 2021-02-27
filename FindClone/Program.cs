using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;

namespace FindClone
{
    class Program
    {
        private static char[] Spinner = {'-', '\\', '|', '/'};

        private static void Clear()
        {
            Console.Write(new string('\r', Console.WindowWidth));
        }

        static int Main(string[] args)
        {
            if (!args.Any())
            {
                Console.WriteLine("A simple utility to find file clones in a specific directory recursively");
                Console.WriteLine("Usage:");
                Console.WriteLine("FindClone.exe c:\\Directory c:\\File.txt");
                return 1;
            }

            var where = args[0];
            if (!Directory.Exists(where))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"directory '{where}' does not exist");
                Console.ResetColor();
                return 1;
            }

            var what = new FileInfo(args[1]);
            if (!what.Exists)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"file '{what}' is not found");
                Console.ResetColor();
                return 2;
            }


            int filesProcessed = 0;
            int found = 0;
            int errors = 0;

            object lockObj = new object();
            int spinnerPosition = 0;
            using (new Timer((_) =>
                {
                    spinnerPosition = (spinnerPosition + 1) % Spinner.Length;

                    lock (lockObj)
                    {
                        Clear();
                        Console.Write($"{Spinner[spinnerPosition]}");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($" PROCESSED: {filesProcessed}");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($" FOUND: {found}");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($" ERRORS: {errors}");
                        Console.ResetColor();
                    }
                }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100)))
            {
                var files = Directory.EnumerateFiles(where, "*.*", SearchOption.AllDirectories)
                    .Select(fileName => new FileInfo(fileName))
                    .Where(file => file.FullName != what.FullName);

                using (var source = new FileStream(what.FullName, FileMode.Open))
                {
                    foreach (var file in files)
                    {
                        source.Seek(0, SeekOrigin.Begin);

                        try
                        {
                            using (var target = new FileStream(file.FullName, FileMode.Open))
                            {
                                if (source.Length != target.Length)
                                {
                                    continue;
                                }

                                var isSame = true;
                                while (target.Position < target.Length)
                                {
                                    if (source.ReadByte() != target.ReadByte())
                                    {
                                        isSame = false;
                                        break;
                                    }
                                }

                                if (isSame)
                                {
                                    lock (lockObj)
                                    {
                                        Clear();
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("Found: " + file.FullName);
                                        Console.ResetColor();
                                        found++;
                                    }
                                }

                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            lock (lockObj)
                            {
                                Clear();
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Error.WriteLine(e.Message);
                                Console.ResetColor();
                                errors++;
                            }
                        }
                        finally
                        {
                            filesProcessed++;
                        }
                    }
                }
            }

            return 0;
        }
    }
}
