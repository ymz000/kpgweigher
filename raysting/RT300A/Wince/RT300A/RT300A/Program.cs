﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
namespace Mndz
{
    //Log changes:
    //2014-7-26 :  add voltage tracking function
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [MTAThread]
        static void Main()
        {
            mainwnd = new Form1();
            msg = new MsgDlg();
            Application.Run(mainwnd);
        }
        /*
         * Change summary : 2015-03-14: use seperate relay for current from 100A to 600A, support 100,200,300,400,500,600,1000 now.
         *                  2015-07-09: add 6000A support and use direct relay for 6000A
         */
        internal static Form1 mainwnd;
        internal static MsgDlg msg;
        internal static void Debug(string line)
        {
            MessageBox.Show(line); //by sojo
        }
        internal static void MsgShow(string line)
        {
            Program.msg.Init(line);
        }
        public static void Upgrade()
        {
            string diskdir = "";
            if (Directory.Exists(GlobalConfig.udiskdir) && File.Exists(GlobalConfig.udiskdir + @"\RT300A.exe"))
                diskdir = GlobalConfig.udiskdir;
            if (Directory.Exists(GlobalConfig.udiskdir2) && File.Exists(GlobalConfig.udiskdir2 + @"\RT300A.exe"))
                diskdir = GlobalConfig.udiskdir2;
            if (diskdir != "")
            {

                Process app = new Process();
                app.StartInfo.WorkingDirectory = GlobalConfig.basedir;
                app.StartInfo.FileName = GlobalConfig.basedir + @"\CEUpgrade.exe";
                app.StartInfo.Arguments = "\"/from:" + diskdir + "\\RT300A.exe\" \"/to:" + GlobalConfig.basedir + "\\tsioex.exe\"";
                app.Start();
                Process.GetCurrentProcess().Kill();
                return;
            }
            

        }

    }
}