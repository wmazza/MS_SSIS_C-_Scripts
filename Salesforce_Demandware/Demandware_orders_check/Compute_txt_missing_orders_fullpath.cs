using System;
using System.Data;
using Microsoft.SqlServer.Dts.Runtime;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Collections.Generic;


namespace ST_187b9359007b490a8780c0a1b8c1fc83
{
	[Microsoft.SqlServer.Dts.Tasks.ScriptTask.SSISScriptTaskEntryPointAttribute]
	public partial class ScriptMain : Microsoft.SqlServer.Dts.Tasks.ScriptTask.VSTARTScriptObjectModelBase
	{
		public void Main()
		{
            try { 
                string path_missing_orders_files = Dts.Variables["User::path_missing_orders_files"].Value.ToString();
                List<string> list_missing_orders_fullpaths = new List<string>();
                string txt_files_concatenated = "";

                DirectoryInfo dir = new DirectoryInfo(path_missing_orders_files);
                FileInfo[] files_info = dir.GetFiles("*.txt").OrderByDescending(f => f.LastWriteTime).ToArray(); //.Where(f => !f.Name.Contains("merged"))

                if (files_info.Count() == 0)
                {
                    Dts.Variables["User::flag_missing_orders_txt"].Value = false;

                }
                else
                {
                    foreach (FileInfo file_info in files_info)
                    {
                        string file_fullpath = file_info.FullName;
                        list_missing_orders_fullpaths.Add(file_fullpath);
                        txt_files_concatenated += file_fullpath + Environment.NewLine;
                    }

                    Dts.Variables["User::list_missing_orders_fullpaths"].Value = list_missing_orders_fullpaths;
                    Dts.Variables["User::txt_files_concatenated"].Value = txt_files_concatenated;
                }

                Dts.TaskResult = (int)ScriptResults.Success;
            }
            catch(Exception ex)
            {
                //MessageBox.Show(ex.Message);
                Dts.TaskResult = (int)ScriptResults.Failure;
            }

        }
	}
}