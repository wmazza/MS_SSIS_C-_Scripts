using System;
using System.Data;
using Microsoft.SqlServer.Dts.Runtime;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Xml;

namespace ST_100c65ff36b94145b5d079cdfc4e5ae7
{
	[Microsoft.SqlServer.Dts.Tasks.ScriptTask.SSISScriptTaskEntryPointAttribute]
	public partial class ScriptMain : Microsoft.SqlServer.Dts.Tasks.ScriptTask.VSTARTScriptObjectModelBase
	{       
		public void Main()
		{

            // Setting Security protocol before sending the request
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string order_search_url = Dts.Variables["User::endpoint"].Value.ToString(); //URL for the request

            string start_datetime = Dts.Variables["User::start_datetime"].Value.ToString(); //start datetime for DateRangeFiltered query
            string end_datetime = Dts.Variables["User::end_datetime"].Value.ToString(); //end datetime for DateRangeFiltered query

            // When start_datetime string in input is equal to last_day we compute start and end date range as previous day
            if(start_datetime == "last_day")
            {
                start_datetime = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd") + "_00:00:00";
                end_datetime = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd") + "_23:59:59";
                Dts.Variables["User::start_datetime"].Value = start_datetime;
                Dts.Variables["User::end_datetime"].Value = end_datetime;
            }

            // Format for xml body for API request to Salesforce Demandware
            string from_datetime = start_datetime.Replace("_", "T") + "Z";
            string to_datetime = end_datetime.Replace("_", "T") + "Z";

            string token = Authentication();
            if(token == "ERROR" || token == "EXCEPTION") {
                Dts.TaskResult = (int)ScriptResults.Failure;
            }

            try
            {
                string body_request_xml_total = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?><root><query><filtered_query><query><filtered_query><query><match_all_query/></query><filter><range_filter><field>creation_date</field><from>" + from_datetime + "</from><to>" + to_datetime + "</to><from_inclusive>true</from_inclusive><to_inclusive>true</to_inclusive></range_filter></filter></filtered_query></query><filter><range_filter><field>creation_date</field><from>2017-12-31T07:00:00Z</from><from_inclusive>true</from_inclusive></range_filter></filter></filtered_query></query><count>1</count><select>(total)</select><sorts><sort><field>creation_date</field><sort_order>asc</sort_order></sort></sorts><start>0</start></root>";

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(order_search_url);
                httpWebRequest.ContentType = "application/xml";
                httpWebRequest.Method = "POST";
                httpWebRequest.Headers.Add("Authorization", "Bearer " + token);

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(body_request_xml_total);
                    streamWriter.Flush();
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                string orders_result = null;

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    orders_result = streamReader.ReadToEnd();
                }

                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    XmlDocument response_document_xml = new XmlDocument();
                    response_document_xml.LoadXml(orders_result);

                    int total = int.Parse(response_document_xml.GetElementsByTagName("total")[0].InnerText);
                    Dts.Variables["User::total_orders_demandware"].Value = total;

                    Dts.TaskResult = (int)ScriptResults.Success;
                }


            }
            catch (Exception ex)
            {
                // MessageBox.Show(ex.Message);
                Dts.Variables["User::exception_message_order_search"].Value = ex.Message;
                Dts.TaskResult = (int)ScriptResults.Failure;
            }            
		}

        private string Authentication()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string auth_url = Dts.Variables["User::authentication_url"].Value.ToString();
            string username = Dts.Variables["$Package::username"].Value.ToString();
            string password = Dts.Variables["$Package::password"].GetSensitiveValue().ToString();

            try
            {
                string encoded = Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(auth_url);
                httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                httpWebRequest.Method = "POST";
                httpWebRequest.Headers.Add("Authorization", "Basic " + encoded);

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string empty_json_body = "";

                    streamWriter.Write(empty_json_body);
                    streamWriter.Flush();
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                string authentication_result = null;
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    authentication_result = streamReader.ReadToEnd();
                }

                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    // Extracting the token from response (json format but saved as string)
                    string[] pairs = authentication_result.Split(',');
                    string[] bearer_token_raw = pairs[0].Split(':');
                    string bearer_token = bearer_token_raw[1].Replace("\"", "");

                    return bearer_token;
                }
                else
                {
                    return "ERROR";
                }               
            }

            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                Dts.Variables["User::exception_message_authentication"].Value = ex.Message;
                return "EXCEPTION";
            }
        }
	}
}