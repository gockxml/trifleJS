﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using Noesis.Javascript;
using Microsoft.Win32;

namespace TrifleJS
{
    class Program
    {
        public static JavascriptContext context;
        public static bool verbose = false;

        [STAThread]
        static void Main(string[] args)
        {
            // Usage
            if (args.Length < 1 || args[0] == "--help") {
                Usage();
                return;
            }

#if DEBUG
            Program.verbose = true;
#endif

            // First Loop - Config (ie Set version etc)
            bool isVersionSet = false;
            foreach (string arg in args)
            {
                string[] parts = arg.Split(':');
                switch (parts[0])
                {
                    case "--verbose":
                        Program.verbose = true;
                        break;
                    case "--version":
                        string version = arg.Replace("--version:", "");
                        isVersionSet = true;
                        switch (version.ToUpper())
                        {
                            case "IE7":
                                Browser.SetIE7();
                                break;
                            case "IE8":
                                Browser.SetIE8();
                                break;
                            case "IE9":
                                Browser.SetIE9();
                                break;
                            case "IE10":
                                Browser.SetIE10();
                                break;
                            default:
                                Console.Error.WriteLine(String.Format("Unrecognized IE Version \"{0}\". Choose from \"IE7\", \"IE8\", \"IE9\", \"IE10\".", version));
                                break;
                        }
                        break;
                }
            }

            // Default to IE9
            if (!isVersionSet)
            {
                Browser.SetIE9();
            }

            // Second Loop - Execute Commands
            foreach (string arg in args) {
                string[] parts = arg.Split(':');
                switch (parts[0]) 
                { 
                    // Self test
                    case "--test":
                        Test();
                        break;
                    case "--render":
                        string url = arg.Replace("--render:", "");
                        Render(url);
                        break;
                    case "--open":
                        string filename = arg.Replace("--open:", "");
                        Open(filename);
                        break;
                }
            }

            #if DEBUG
            // Debugging? Wait for input
            Console.Read();
            #endif
        }

        static void Usage() {
            Console.WriteLine("Usage: triflejs.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options: ");
            Console.WriteLine("  --help          Show this message.");
            Console.WriteLine("  --test          Runs a System Test.");
            Console.WriteLine("  --render:url    Opens a url and renders into a file.");
            Console.WriteLine("  --open:file     Opens a file and executes in V8 engine API.");

        }

        static void Test() { 
        
        }

        static void Render(string url) {
            Console.WriteLine("Rendering " + url + "...");

            // Check the URL
            Uri uri;
            try
            {
                uri = new Uri(url);
            }
            catch
            {
                Console.Error.WriteLine("Unable to open url: " + url);
                return;
            }

            // Continue if ok
            using (var browser = new Browser())
            {
                browser.Size = new Size(1024, 700);
                browser.Navigate(url); //a file or a url
                browser.ScrollBarsEnabled = false;
                browser.RenderOnLoad(uri.Host + ".png");

                while (browser.ReadyState != System.Windows.Forms.WebBrowserReadyState.Complete)
                {
                    System.Windows.Forms.Application.DoEvents();
                }
            }
        }

        static void Open(string filename) {
            if (!File.Exists(filename))
            {
                Console.Error.WriteLine(String.Format("File does not exist: {0}", filename));
                return;
            }

            //Initialize a context
            using (Program.context = new JavascriptContext()) {

                // Setting external parameters for the context
                context.SetParameter("console", new Host.console());
                context.SetParameter("triflejs", new Host.triflejs());
                context.SetParameter("interop", new Host.interop());

                try
                {
                    // Initialise host env
                    context.Run(TrifleJS.Properties.Resources.triflejs_core, "triflejs.core.js");
                    context.Run(TrifleJS.Properties.Resources.triflejs_modules, "triflejs.modules.js");
                    context.Run(TrifleJS.Properties.Resources.triflejs_init, "triflejs.init.js");

                    // Run the script
                    context.Run(File.ReadAllText(filename), (new FileInfo(filename)).Name);
                }
                catch (Noesis.Javascript.JavascriptException ex)
                {
                    Host.Handle(ex);
                }
                catch (Exception ex) {
                    // Error in C#!
                    Console.Error.WriteLine(String.Format("\n===================\n{0} {1}\n===================\n{2}", ex.GetType().Name, ex.Message, ex.StackTrace));
                }

                // Getting a parameter
                //Console.WriteLine("number: " + context.GetParameter("number"));
            }
        }
    }
}