using System;
using System.Data;
using Microsoft.SqlServer.Dts.Runtime;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace ST_100c65ff36b94145b5d079cdfc4e5ae7
{
    
	[Microsoft.SqlServer.Dts.Tasks.ScriptTask.SSISScriptTaskEntryPointAttribute]
	public partial class ScriptMain : Microsoft.SqlServer.Dts.Tasks.ScriptTask.VSTARTScriptObjectModelBase
	{
		public void Main()
		{

            // Setting Security protocol before sending the request
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //string token = Dts.Variables["User::bearer_token"].Value.ToString(); //Bearer token obtained by the Authentication task
            string order_search_url = Dts.Variables["User::endpoint"].Value.ToString(); //URL for the request

            string start_datetime = Dts.Variables["User::start_datetime"].Value.ToString();
            string end_datetime = Dts.Variables["User::end_datetime"].Value.ToString();

            string from_datetime = start_datetime.Replace("_", "T") + "Z";
            string to_datetime = end_datetime.Replace("_", "T") + "Z";

            int orders_retrieved = 0;
            int total_orders_demandware = (int)Dts.Variables["User::total_orders_demandware"].Value;
            int start = 0;
            int num_iteration = 0;
            List<string> order_no_list = new List<string>();

            string token = "";
            int start_validity = 0;
            int error_counter = 0; //count errors

            try
            {
                while(orders_retrieved < total_orders_demandware) {

                    token = AuthenticationWithValidityCheck(ref start_validity, token);
                    if (token == "ERROR" || token == "EXCEPTION")
                    {
                        Dts.TaskResult = (int)ScriptResults.Failure;
                    }

                    //string body_request_xml_order_no = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?><root><query><filtered_query><query><filtered_query><query><match_all_query/></query><filter><range_filter><field>creation_date</field><from>" + from_datetime + "</from><to>" + to_datetime + "</to><from_inclusive>true</from_inclusive><to_inclusive>true</to_inclusive></range_filter></filter></filtered_query></query><filter><range_filter><field>creation_date</field><from>2017-12-31T07:00:00Z</from><from_inclusive>true</from_inclusive></range_filter></filter></filtered_query></query><count>200</count><select><select>(hits.(data.(order_no)),count)</select></select><sorts><sort><field>creation_date</field><sort_order>asc</sort_order></sort></sorts><start>" + start.ToString() + "</start></root>";
                    string body_request_xml_order_no = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?><root><query><filtered_query><query><filtered_query><query><match_all_query/></query><filter><range_filter><field>creation_date</field><from>" + from_datetime + "</from><to>" + to_datetime + "</to><from_inclusive>true</from_inclusive><to_inclusive>true</to_inclusive></range_filter></filter></filtered_query></query><filter><range_filter><field>creation_date</field><from>2017-12-31T07:00:00Z</from><from_inclusive>true</from_inclusive></range_filter></filter></filtered_query></query><count>200</count><select>(hits.(data.(order_no)),count)</select><sorts><sort><field>creation_date</field><sort_order>asc</sort_order></sort></sorts><start>" + start + "</start></root>";

                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(order_search_url);
                    httpWebRequest.ContentType = "application/xml";
                    httpWebRequest.Method = "POST";
                    httpWebRequest.Headers.Add("Authorization", "Bearer " + token);
                   
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(body_request_xml_order_no);
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

                        XmlNodeList order_no_listnode = response_document_xml.GetElementsByTagName("order_no");

                        foreach(XmlNode order_no_node in order_no_listnode)
                        {
                            order_no_list.Add(order_no_node.InnerText);
                            orders_retrieved += 1;
                        }


                        int count = int.Parse(response_document_xml.GetElementsByTagName("count")[0].InnerText);                        

                        num_iteration += 1;
                        start = 200 * num_iteration;

                        //MessageBox.Show("NUM_ITERATION: " + num_iteration.ToString() + "\nstart: " + start.ToString() + "\norders_retrieved: " + orders_retrieved.ToString() + "\nTotal demandware: " + total_orders_demandware.ToString());
                    
                    }

                }

                Dts.Variables["User::order_no_list_demandware"].Value = order_no_list;

                Dts.TaskResult = (int)ScriptResults.Success;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                //MessageBox.Show("NUM_ITERATION: " + num_iteration.ToString());
                string exception_message = Dts.Variables["User::exception_message_order_search"].Value.ToString();
                exception_message += "; " + ex.Message;
                Dts.Variables["User::exception_message_order_search"].Value = exception_message;

                error_counter++;
                if(error_counter == 10) Dts.TaskResult = (int)ScriptResults.Failure;
            }            
		}

        private string AuthenticationWithValidityCheck(ref int start_validity, string token)
        {
            int current_time = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            int difference = current_time - start_validity;

            if(token == "" || difference > 1740) {

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string auth_url = Dts.Variables["User::authentication_url"].Value.ToString();
                string username = Dts.Variables["$Package::username"].Value.ToString();
                string password = Dts.Variables["$Package::password"].GetSensitiveValue().ToString();
                start_validity = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

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
                        //Extracting the token from response (json format but saved as string)
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
            else
            {
                return token;
            }
        }
	}
}