﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BatteryPerserve
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
		//this is a test comment
		//this is a test comment
		//this is a test comment
		//this is a test comment
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BatteryOptimizer());


        }
    }
}
