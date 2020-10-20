using System;
using System.Data;
using System.Linq;
using Microsoft.SqlServer.Dts.Runtime;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Data.OleDb;

namespace ST_70e93ed91d074dbd85e46135a411db75
{
	[Microsoft.SqlServer.Dts.Tasks.ScriptTask.SSISScriptTaskEntryPointAttribute]
	public partial class ScriptMain : Microsoft.SqlServer.Dts.Tasks.ScriptTask.VSTARTScriptObjectModelBase
	{
		public void Main()
		{

            try { 
                List<string> order_no_list_demandware = (List<string>)Dts.Variables["User::order_no_list_demandware"].Value;
                string path_save_missing_order_file = Dts.Variables["User::path_save_missing_order_file"].Value.ToString();
                string order_no_missing = "";

                DataTable dt = new DataTable();
                OleDbDataAdapter oleDa = new OleDbDataAdapter();
                oleDa.Fill(dt, Dts.Variables["User::order_no_list_db"].Value);

                //List<string> order_no_list_db = (List<string>)Dts.Variables["User::order_no_list_db"].Value;
                List<string> order_no_list_db = dt.Rows.OfType<DataRow>().Select(dr => (string)dr["ORDER_NO"]).ToList();

                //foreach(DataRow row in dt.Rows)
                foreach (string order_no_dmw in order_no_list_demandware)
                {
                    if (order_no_list_db.Contains(order_no_dmw))
                    {
                        //MessageBox.Show("CONTAINS: " + order_no_dmw);
                    }
                    else
                    {
                        //MessageBox.Show("NOT CONTAINS: " + order_no_dmw);
                        order_no_missing += order_no_dmw + "\n";
                    }
                }
                if(order_no_missing != "")
                {
                    System.IO.File.WriteAllText(path_save_missing_order_file, order_no_missing);
                    Dts.Variables["User::order_no_missing_concatenated"].Value = order_no_missing;
                }
            }
            catch(Exception ex)
            {
                // MessageBox.Show(ex.Message);
            }

            Dts.TaskResult = (int)ScriptResults.Success;
		}
	}
}