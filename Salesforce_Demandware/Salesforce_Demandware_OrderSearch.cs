namespace ST_d8c357173905409d85dc3ebbfd7c7ea3
{
    
	public partial class ScriptMain : Microsoft.SqlServer.Dts.Tasks.ScriptTask.VSTARTScriptObjectModelBase
	{
		public void Main()
		{                   
            //Setting Security protocol before sending the request
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //Pause for memory management
            System.Threading.Thread.Sleep(1000);

            string token = Dts.Variables["User::bearer_token"].Value.ToString(); //Bearer token obtained by the Authentication task
            string order_search_url = Dts.Variables["User::endpoint"].Value.ToString(); //URL for the request
            string xmls_save_path = Dts.Variables["User::xmls_save_path"].Value.ToString(); //base path for saving XMLs
            string orders_requested_count = Dts.Variables["User::orders_requested_count"].Value.ToString(); //The number of returned documents  

            string flag_endpoint = Dts.Variables["User::flag_endpoint"].Value.ToString();
            string field_to_filter_on = Dts.Variables["User::field_to_filter_on"].Value.ToString();
            string flag_field_to_filter_on = Dts.Variables["User::flag_field_to_filter_on"].Value.ToString();
            string date_range_type_extended = Dts.Variables["User::date_range_type_extended"].Value.ToString();

            int iteration_num = (int)Dts.Variables["User::iteration_number"].Value; //Iterations counter
            bool flag_hexadecimal = (bool)Dts.Variables["flag_hexadecimal"].Value; //Flag to remove hexadecimal chars in case of exception thrown

            int total_orders_current_value = (int)Dts.Variables["User::total_orders_current_value"].Value; //Latest total value returned (needed for dynamic offset approach)
            int start = 0; //offset value to requets data           
            List<string> order_no_list_demandware = (List<string>)Dts.Variables["User::order_no_list_demandware"].Value; //List of order_no retrieved before actual API Extract



            int id_where_exception_occured = -111; //Value to track where exception was thrown
            string additional_exception_info = "";

            try
            {

                id_where_exception_occured = -1; 

                //Setting the body for the request according to the field to filter on
                string body_request_xml = "";
                string filename = "";

                if (flag_field_to_filter_on == "O" || flag_field_to_filter_on == "CN")
                {
                    start = int.Parse(orders_requested_count) * iteration_num;

                    string search_phrase_for_field_filter = Dts.Variables["User::search_phrase_for_field_filter"].Value.ToString();
                    body_request_xml = "<?xml version=\"1.0\" encoding=\"UTF - 8\" ?><root><query><text_query><fields><field>" + field_to_filter_on + "</field></fields><search_phrase>" + search_phrase_for_field_filter + "</search_phrase></text_query></query><count>" + orders_requested_count + "</count> <select>(**)</select><start>" + start + "</start></root>";

                    string which_field = ""; //Used to set the filename
                    if (flag_field_to_filter_on == "O") which_field = "ORDER_NO";
                    else if (flag_field_to_filter_on == "CN") which_field = "CUSTOMER_NO";

                    filename = "XML_" + DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss") + "_" + flag_endpoint + "_" + which_field +"_" + search_phrase_for_field_filter + ".xml";
                }
                else if (flag_field_to_filter_on == "M")// || flag_field_to_filter_on == "C")
                {
                    string from_datetime = Dts.Variables["User::from_datetime"].Value.ToString(); //Starting date i.e. "2019-07-26T00:00:00Z" 
                    string to_datetime = Dts.Variables["User::to_datetime"].Value.ToString(); //Ending date i.e. "2019-07-27T00:00:00.000Z"

                    //set current total orders on first iteration and start for next ones for dynamic offset approach
                    if (iteration_num == 0) { total_orders_current_value = (int)Dts.Variables["User::total_orders_demandware"].Value; }
                    else { start = (int)Dts.Variables["User::start"].Value; }

                    body_request_xml = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?><root><query><filtered_query><query><filtered_query><query><match_all_query/></query><filter><range_filter><field>" + field_to_filter_on + "</field><from>" + from_datetime + "</from><to>" + to_datetime + "</to><from_inclusive>true</from_inclusive><to_inclusive>true</to_inclusive></range_filter></filter></filtered_query></query><filter><range_filter><field>creation_date</field><from>2017-12-31T07:00:00Z</from><from_inclusive>true</from_inclusive></range_filter></filter></filtered_query></query><count>" + orders_requested_count + "</count><select>(**)</select><sorts><sort><field>creation_date</field><sort_order>asc</sort_order></sort></sorts><start>" + start + "</start></root>";
                    filename = "XML_"+ DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss") + "_" + flag_endpoint + "_" + flag_field_to_filter_on + "_N" + iteration_num + "_" + from_datetime.Replace(':', '-').Substring(0, from_datetime.Length - 5) + "_" + to_datetime.Replace(':', '-').Substring(0, to_datetime.Length - 5) + ".xml";
                }
                else if (flag_field_to_filter_on == "C")
                {
                    start = int.Parse(orders_requested_count) * iteration_num;

                    string from_datetime = Dts.Variables["User::from_datetime"].Value.ToString(); //Starting date i.e. "2019-07-26T00:00:00Z" 
                    string to_datetime = Dts.Variables["User::to_datetime"].Value.ToString(); //Ending date i.e. "2019-07-27T00:00:00.000Z"

                    body_request_xml = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?><root><query><filtered_query><query><match_all_query/></query><filter><range_filter><field>" + field_to_filter_on + "</field><from>" + from_datetime + "</from><to>" + to_datetime + "</to><from_inclusive>true</from_inclusive><to_inclusive>true</to_inclusive></range_filter></filter></filtered_query></query><count>" + orders_requested_count + "</count><select>(**)</select><sorts><sort><field>last_modified</field><sort_order>desc</sort_order></sort></sorts><start>" + start + "</start></root>";

                    filename = "XML_" + DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss") + "_" + flag_endpoint + "_" + flag_field_to_filter_on + "_N" + iteration_num + "_" + from_datetime.Replace(':', '-').Substring(0, from_datetime.Length - 5) + "_" + to_datetime.Replace(':', '-').Substring(0, to_datetime.Length - 5) + ".xml";
                }
                

                //Start request code
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(order_search_url);
                httpWebRequest.ContentType = "application/xml";
                httpWebRequest.Method = "POST";
                httpWebRequest.Headers.Add("Authorization", "Bearer " + token);

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(body_request_xml);
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
                    id_where_exception_occured = 0;

                    orders_result = orders_result.Replace("c:", "");
                    orders_result = orders_result.Replace("xsd:", "");

                    //replace payment_instrument_id with payment_instrument_id_xml for SSIS issue on same name elements
                    orders_result = orders_result.Replace("payment_instrument_id", "payment_instrument_id_xml");

                    //replace old tagnames (before radial implementation) to new ones
                    orders_result = orders_result.Replace("c_browser-accept", "c_radialFraudBrowserAccept");
                    orders_result = orders_result.Replace("c_browser-accept-encoding", "c_radialFraudBrowserAcceptEncoding");
                    orders_result = orders_result.Replace("c_browser-connection", "c_radialFraudBrowserConnection");
                    orders_result = orders_result.Replace("c_browser-cookie", "c_radialFraudBrowserCookie");
                    orders_result = orders_result.Replace("c_browser-id", "c_radialFraudBrowserID");
                    orders_result = orders_result.Replace("c_browser-id-language-code", "c_radialFraudBrowserIdLanguageCode");
                    orders_result = orders_result.Replace("c_browser-referer", "c_radialFraudBrowserReferer");
                    orders_result = orders_result.Replace("c_browser-session-id", "c_radialFraudBrowserSessionId");
                    orders_result = orders_result.Replace("c_device-id", "c_radialFraudDeviceID");
                    orders_result = orders_result.Replace("c_raw-cookie", "c_radialFraudRawCookie");
                    orders_result = orders_result.Replace("c_time-spent-on-site", "c_radialFraudTimeSpentOnSite");
                    
                    //If there was an error because of hexadecimal value we parse the response string to remove illegal chars
                    if(flag_hexadecimal == true)
                    {
                        var validXmlChars = orders_result.Where(ch => XmlConvert.IsXmlChar(ch)).ToArray();
                        orders_result = new string(validXmlChars);

                        Dts.Variables["flag_hexadecimal"].Value = false;
                    }

                    XmlDocument original_document_xml = new XmlDocument();
                    original_document_xml.LoadXml(orders_result);
                    Dts.Variables["User::ordersearch_hits_count"].Value = original_document_xml.GetElementsByTagName("count")[0].InnerText;
                    XmlNode order_search_result_element = original_document_xml.GetElementsByTagName("order_search_result")[0];
                    order_search_result_element.Attributes.RemoveAll();

                    //Define namespace for SelectSingleNode function
                    string namespace_string = "urn:demandware.com:shop:order_search_result:19.10";
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(original_document_xml.NameTable);
                    nsmgr.AddNamespace("ns", namespace_string);

                    id_where_exception_occured = 1;
                    //1. c_orderStatusJSON to XML
                    XmlNodeList orderStatusJSON_List = original_document_xml.GetElementsByTagName("c_orderStatusJSON");

                    for (int i = 0; i < orderStatusJSON_List.Count; i++)
                    {
                        XmlDocument order_status_json_xml = new XmlDocument();
                        XmlDocument order_status_json_xml_fixed = new XmlDocument();

                        //Take orderStatusJSON nodes and convert in xml its content to xmldocument
                        XmlNode order_status_json_node = orderStatusJSON_List[i];   

                        string json = order_status_json_node.InnerText;
                        order_status_json_xml = JsonConvert.DeserializeXmlNode(json, "c_orderStatusJSON");

                        XmlNode elem_c_orderStatusJSON = order_status_json_xml_fixed.CreateElement(order_status_json_node.Name, namespace_string);
                        order_status_json_xml_fixed.AppendChild(elem_c_orderStatusJSON);

                        XmlNode root_order_status_json_xml = order_status_json_xml.DocumentElement;
                        XmlNode root_order_status_json_xml_fixed = order_status_json_xml_fixed.DocumentElement;

                        foreach (XmlNode child in root_order_status_json_xml.ChildNodes)
                        {
                            string node_name = child.Name;

                            XmlElement order_status_elem = order_status_json_xml_fixed.CreateElement("order_status", namespace_string);
                            order_status_elem.SetAttribute("id", node_name);
                            root_order_status_json_xml_fixed.AppendChild(order_status_elem);

                            foreach (XmlNode child2 in child.ChildNodes)
                            {
                                //XmlNode content_node = order_status_json_xml_fixed.ImportNode(child2, true);
                                //order_status_elem.AppendChild(content_node);
                                XmlNode content_node = order_status_json_xml_fixed.CreateElement(child2.Name, namespace_string);
                                content_node.InnerText = child2.InnerText;
                                order_status_elem.AppendChild(content_node);
                            }
                        }
                        order_status_json_node.ParentNode.ReplaceChild(original_document_xml.ImportNode(root_order_status_json_xml_fixed, true), order_status_json_node);
                    }

                    id_where_exception_occured = 2;
                    //2. c_statusjson to XML
                    XmlNodeList statusjson_List = original_document_xml.GetElementsByTagName("c_statusjson");

                    for (int i = 0; i < statusjson_List.Count; i++)
                    {
                        XmlDocument status_json_xml = new XmlDocument();
                        XmlDocument status_json_xml_fixed_chrono = new XmlDocument();

                        //Take orderStatusJSON nodes and convert in xml its content to xmldocument
                        XmlNode status_json_node = statusjson_List[i];  

                        string json = status_json_node.InnerText;
                        status_json_xml = JsonConvert.DeserializeXmlNode(json, "c_statusjson");

                        XmlNode chrono_node = status_json_xml.GetElementsByTagName("chrono")[0];
                        //XmlNode statusArr = chrono_node.ChildNodes[0];

                        if (chrono_node != null) {
                            XmlNodeList statusArr_list = chrono_node.SelectNodes(".//statusArr", nsmgr);

                            XmlNode elem_c_statusjson = status_json_xml_fixed_chrono.CreateElement(status_json_node.Name, namespace_string);
                            status_json_xml_fixed_chrono.AppendChild(elem_c_statusjson);
                            XmlNode root_status_json_xml_fixed_chrono = status_json_xml_fixed_chrono.DocumentElement;

                            XmlNode content_node = status_json_xml_fixed_chrono.CreateElement(chrono_node.Name, namespace_string);

                            foreach (XmlNode statusArr in statusArr_list)
                            {
                                XmlNode statusArr_node_new = status_json_xml_fixed_chrono.CreateElement(statusArr.Name, namespace_string);

                                foreach (XmlNode child2 in statusArr.ChildNodes)
                                {
                                    if (child2.InnerText != "")
                                    {
                                        XmlNode content_node_child = status_json_xml_fixed_chrono.CreateElement(child2.Name, "urn:demandware.com:shop:order_search_result:19.10");
                                        content_node_child.InnerText = child2.InnerText;
                                        statusArr_node_new.AppendChild(content_node_child);
                                    }
                                }
                                content_node.AppendChild(statusArr_node_new);
                            }
                            root_status_json_xml_fixed_chrono.AppendChild(content_node);

                            status_json_node.ParentNode.ReplaceChild(original_document_xml.ImportNode(root_status_json_xml_fixed_chrono, true), status_json_node);
                        }
                    }

                    id_where_exception_occured = 3;
                    //3. Enhance tax_basis, tax_rate and tax_class_id
                    XmlNodeList taxbasis_List = original_document_xml.GetElementsByTagName("tax_basis");
                    XmlNodeList taxclassid_List = original_document_xml.GetElementsByTagName("tax_class_id");
                    XmlNodeList taxrate_List = original_document_xml.GetElementsByTagName("tax_rate");

                    int count_taxbasis = taxbasis_List.Count;
                    int count_taxrate = taxrate_List.Count;

                    for (int i = count_taxbasis - 1; i >= 0; i--)
                    {
                        var taxbasis_value = taxbasis_List[i].InnerText;

                        if (taxbasis_value == "") taxbasis_List[i].ParentNode.RemoveChild(taxbasis_List[i]);
                        else taxbasis_List[i].Attributes.Remove(taxbasis_List[i].Attributes["nil"]);
                    }

                    for (int i = count_taxrate - 1; i >= 0; i--)
                    {
                        var taxrate_value = taxrate_List[i].InnerText;

                        if (taxrate_value == "") taxrate_List[i].ParentNode.RemoveChild(taxrate_List[i]);
                        else taxrate_List[i].Attributes.Remove(taxrate_List[0].Attributes["nil"]);
                    }

                    foreach (XmlNode taxclassid_node in taxclassid_List)
                    {
                        taxclassid_node.Attributes.Remove(taxclassid_node.Attributes["nil"]);
                    }

                    id_where_exception_occured = 4;
                    //4. Check and fix for c_custom_item cut (happening for chars limit of 4000 from Salesforce); strategy is to remove the corrupted code leaving info about order_id
                    XmlNodeList c_orderstatus_List = original_document_xml.GetElementsByTagName("c_orderStatus");

                    foreach (XmlNode c_orderstatusnode in c_orderstatus_List)
                    {
                        XmlNodeList c_custom_item_List = c_orderstatusnode.ChildNodes;

                        foreach (XmlNode c_custom_item_node in c_custom_item_List)
                        {
                            string c_custom_item_inner_text = c_custom_item_node.InnerText;
                            int inner_text_length = c_custom_item_inner_text.Length;

                            c_custom_item_inner_text.Replace("&lt;", '<'.ToString());
                            c_custom_item_inner_text.Replace("&gt;", '>'.ToString());

                            if (inner_text_length >= 4000)
                            {
                                int index_original_id_closing_tag = c_custom_item_inner_text.IndexOf("</original-order-id>");
                                additional_exception_info = "order_no: " + c_custom_item_inner_text.Substring(28, index_original_id_closing_tag - 28);

                                string last_part_inner_text = c_custom_item_inner_text.Substring(inner_text_length - 10, 10);
                                if (last_part_inner_text != "</Message>")
                                {
                                    //int index_order_id_tag = c_custom_item_inner_text.LastIndexOf("<order-id>");
                                    //c_custom_item_node.InnerText = c_custom_item_inner_text.Substring(0, index_order_id_tag) + "<status-type>TRUNCATION ERROR</status-type></Message>";

                                    int index_last_LineItem_closing_tag = c_custom_item_inner_text.LastIndexOf("</LineItem>") + 11; //+ 11 to include also the closing tag itself

                                    string corrected_cut_LineItem_string = "";
                                    if (index_last_LineItem_closing_tag != 4000)
                                    { //corner case when only Message closing tag is missing

                                        string cut_LineItem_string = c_custom_item_inner_text.Substring(index_last_LineItem_closing_tag, (c_custom_item_inner_text.Length - index_last_LineItem_closing_tag) - 1);
                                        int length_cut_LineItem_string = cut_LineItem_string.Length;

                                        //check if there is line-number element at full
                                        int index_linenumber_closing_tag = cut_LineItem_string.LastIndexOf("</line-number>");

                                        if (index_linenumber_closing_tag != -1)
                                        { //full line-number element
                                          //corrected_cut_LineItem_string = cut_LineItem_string.Substring(0, index_linenumber_closing_tag + 14); //+ 14 to include also the closing tag itself
                                            corrected_cut_LineItem_string = cut_LineItem_string.Substring(0, index_linenumber_closing_tag + 14) + "</LineItem>"; //+ 14 to include also the closing tag itself
                                        }
                                        else if (index_linenumber_closing_tag == -1)
                                        { //line-number element is cut
                                            int index_linenumber_opening_tag = cut_LineItem_string.LastIndexOf("<line-number>");
                                            if (index_linenumber_opening_tag != -1)
                                            { //check if opening tag is complete
                                                string temp_partial_cut_LineItem_string = cut_LineItem_string.Substring(index_linenumber_opening_tag + 13, (length_cut_LineItem_string - index_linenumber_opening_tag - 13) - 1);
                                                int index_closing_symbol = temp_partial_cut_LineItem_string.IndexOf("<");
                                                if (index_closing_symbol != -1)
                                                { //check if the line-number value is cut
                                                    string linenumber_value = temp_partial_cut_LineItem_string.Substring(0, index_closing_symbol);
                                                    //corrected_cut_LineItem_string = "<LineItem><line-number>" + linenumber_value + "</line-number>";
                                                    corrected_cut_LineItem_string = "<LineItem><line-number>" + linenumber_value + "</line-number></LineItem>";
                                                }
                                            }
                                        }

                                    }

                                    //c_custom_item_node.InnerText = c_custom_item_inner_text.Substring(0, index_last_LineItem_closing_tag) + corrected_cut_LineItem_string +  "</LineItem></Message>";
                                    //string temp_innerText = c_custom_item_inner_text.Substring(0, index_last_LineItem_closing_tag) + corrected_cut_LineItem_string + "</LineItem></Message>";
                                    string temp_innerText = c_custom_item_inner_text.Substring(0, index_last_LineItem_closing_tag) + corrected_cut_LineItem_string + "</Message>";

                                    
                                    //Change status type
                                    int index_starting_status_type_value = temp_innerText.IndexOf("<status-type>") + 13;
                                    int index_ending_status_type_value = temp_innerText.IndexOf("</status-type>");
                                    string status_type = temp_innerText.Substring(index_starting_status_type_value, index_ending_status_type_value - index_starting_status_type_value);

                                    string new_status_type = "TRUNC ERROR " + status_type;
                                    c_custom_item_node.InnerText = temp_innerText.Substring(0, index_starting_status_type_value) + new_status_type + temp_innerText.Substring(index_ending_status_type_value, temp_innerText.Length - index_ending_status_type_value);

                                }

                            }
                            //Convert innerText to XmlNode to avoid < and > code issue
                            XmlDocument temp_inner_text_as_xmldoc = new XmlDocument();
                            temp_inner_text_as_xmldoc.LoadXml(c_custom_item_node.InnerText);
                            XmlNode temp_inner_text_as_xmlnode = temp_inner_text_as_xmldoc.DocumentElement;

                            c_custom_item_node.RemoveAll();
                            XmlNode importNode = c_custom_item_node.OwnerDocument.ImportNode(temp_inner_text_as_xmlnode, true);
                            XmlNode newnode = original_document_xml.CreateElement("Message", namespace_string);
                            checkNodes(newnode, importNode.ChildNodes, original_document_xml, namespace_string);
                            c_custom_item_node.AppendChild(newnode);

                        }
                    }

                    id_where_exception_occured = 5;
                    //5. lineNumber/shipmentList fix: to avoid having multiple elements in order_status nodes, values for lineNumbers/shipmentList elements are concatenated
                    XmlNodeList order_status_nodes = original_document_xml.GetElementsByTagName("order_status");

                    foreach (XmlNode order_status_node in order_status_nodes)
                    {
                        //lineNumbers
                        XmlNodeList lineNumbers_nodes = order_status_node.SelectNodes(".//ns:lineNumbers", nsmgr);

                        int count_nodes_os = lineNumbers_nodes.Count;

                        string lineNumber_innerTexts_concatenated = "";

                        for (int i = count_nodes_os - 1; i >= 1; i--)
                        {
                            lineNumber_innerTexts_concatenated = "," + lineNumbers_nodes[i].InnerText + lineNumber_innerTexts_concatenated;
                            lineNumbers_nodes[i].ParentNode.RemoveChild(lineNumbers_nodes[i]);
                        }

                        lineNumber_innerTexts_concatenated = lineNumbers_nodes[0].InnerText + lineNumber_innerTexts_concatenated;
                        lineNumbers_nodes[0].InnerText = lineNumber_innerTexts_concatenated;

                        //shipmentList
                        XmlNodeList shipmentList_nodes = order_status_node.SelectNodes(".//ns:shipmentList", nsmgr);

                        int count_nodes2 = shipmentList_nodes.Count; 

                        string shipmentList_innerTexts_concatenated = "";

                        for (int i = count_nodes2 - 1; i >= 1; i--)
                        {
                            shipmentList_innerTexts_concatenated = "," + shipmentList_nodes[i].InnerText + shipmentList_innerTexts_concatenated;
                            shipmentList_nodes[i].ParentNode.RemoveChild(shipmentList_nodes[i]);
                        }

                        shipmentList_innerTexts_concatenated = shipmentList_nodes[0].InnerText + shipmentList_innerTexts_concatenated;
                        shipmentList_nodes[0].InnerText = shipmentList_innerTexts_concatenated;

                    }

                    id_where_exception_occured = 6;
                    //6. change tag name of price_adjustment_id elements to price_adjustment_id_xml to solve issue with name convention in SSIS (only for childrennot from order_price_adjustment)
                    XmlNodeList price_adjustment_id_nodes = original_document_xml.GetElementsByTagName("price_adjustment_id");
                    int count_nodes_pa = price_adjustment_id_nodes.Count;

                    for (int i = count_nodes_pa - 1; i >= 0; i--)
                    {
                        if (price_adjustment_id_nodes[i].ParentNode.Name != "order_price_adjustment")
                        {
                            string paid_innerText = price_adjustment_id_nodes[i].InnerText;

                            XmlNode new_price_adjustment_id = original_document_xml.CreateElement("price_adjustment_id_xml", namespace_string);
                            new_price_adjustment_id.InnerText = paid_innerText;

                            XmlNode parent = price_adjustment_id_nodes[i].ParentNode;
                            parent.ReplaceChild(new_price_adjustment_id, price_adjustment_id_nodes[i]);

                        }
                    }

                    id_where_exception_occured = 7;
                    //7. change tag name of shipment_id elements to shipment_id_xml to solve issue with name convention in SSIS
                    XmlNodeList shipment_id_nodes = original_document_xml.GetElementsByTagName("shipment_id");
                    int count_nodes_s = shipment_id_nodes.Count;

                    for (int i = count_nodes_s - 1; i >= 0; i--)
                    {
                        if (shipment_id_nodes[i].ParentNode.Name == "shipment")
                        {
                            string sid_innerText = shipment_id_nodes[i].InnerText;

                            XmlNode new_price_adjustment_id = original_document_xml.CreateElement("shipment_id_xml", namespace_string);
                            new_price_adjustment_id.InnerText = sid_innerText;

                            XmlNode parent = shipment_id_nodes[i].ParentNode;
                            parent.ReplaceChild(new_price_adjustment_id, shipment_id_nodes[i]);
                        }
                    }

                    id_where_exception_occured = 8;
                    //8. replace c_eaemployeeID to c_associateEmail in case it contains and email address
                    XmlNodeList associateID_List = original_document_xml.GetElementsByTagName("c_associateID");
                    foreach (XmlNode associateID_node in associateID_List)
                    {
                        XmlNode data_node = associateID_node.ParentNode;
                        string associateID_innerText = associateID_node.InnerText;

                        if (associateID_innerText.Contains("@"))
                        //if (!IsAllDigits(associateID_node.InnerText))
                        {
                            XmlNode associateEmail = original_document_xml.CreateElement("c_associateEmail", namespace_string);
                            associateEmail.InnerText = associateID_node.InnerText;
                            data_node.AppendChild(associateEmail);
                            associateID_node.InnerText = null;
                        }
                        else if (IsAllDigits(associateID_innerText))
                        {
                            XmlNode employeeID = data_node.SelectSingleNode(".//ns:c_eaEmployeeId", nsmgr);
                            if (employeeID != null) data_node.RemoveChild(employeeID);
                        }
                    }

                    //Compute fullpath to save 
                    string full_file_path = "";
                    if (date_range_type_extended != "Dynamic")
                    {
                        full_file_path = xmls_save_path + "\\" + flag_endpoint + "\\" + date_range_type_extended + "\\" + "To_Merge" + "\\" + filename;
                    }
                    else
                    {
                        string dynamic_subfolder = Dts.Variables["User::dynamic_subfolder"].Value.ToString(); //Default "Misc", if BO "To_Merge";
                        full_file_path = xmls_save_path + "\\" + flag_endpoint + "\\" + date_range_type_extended + "\\" + dynamic_subfolder + "\\" + filename;
                    }
                        

                    original_document_xml.Save(full_file_path);
                    Dts.Variables["User::current_filename"].Value = filename;

                    //Add filename to list_filenames and filenames_concatenated
                    List<string> list_filenames = (List<string>)Dts.Variables["User::list_filenames"].Value;
                    list_filenames.Add(filename);
                    Dts.Variables["User::list_filenames"].Value = list_filenames;
                    Dts.Variables["User::filenames_concatenated"].Value = Dts.Variables["User::filenames_concatenated"].Value.ToString() + filename + ";";


                    id_where_exception_occured = 9;
                    //9. add order_no and orderSSIS to List to populate XML_History table
                    string order_no_concatenated = ""; 

                    XmlNodeList orderno_list = original_document_xml.GetElementsByTagName("order_no");
                    foreach(XmlNode orderno_node in orderno_list)
                    {
                        string orderno_value = orderno_node.InnerText;
                        order_no_concatenated = order_no_concatenated + orderno_value + ";";
                        order_no_list_demandware.RemoveAll(item_order_no => item_order_no == orderno_value);
                    }
                    Dts.Variables["User::order_no_concatenated"].Value = order_no_concatenated;
                   
                    //Get total value from response
                    string total_orders_count = original_document_xml.GetElementsByTagName("total")[0].InnerText;

                    //Set next value for start in case of filter on last_modified
                    if(flag_field_to_filter_on == "M") {

                        //string to_show = "start: " + start + "\ntot_current: " + total_orders_current_value + "\ntot_extracted: " + total_orders_count; 

                        int difference_totals = total_orders_current_value - int.Parse(total_orders_count);

                        //If total orders number raise from previous value it means to_dateimte is set to a future datetime OR DB behavior is unexpected
                        if (difference_totals < 0)
                        {
                            Dts.TaskResult = (int)ScriptResults.Failure;
                        }
                        start = start + int.Parse(orders_requested_count) - difference_totals;

                        Dts.Variables["User::start"].Value = start;
                        Dts.Variables["User::total_orders_current_value"].Value = int.Parse(total_orders_count);

                        //to_show += "\nnew_start: " + start + "\ndifference: " + difference_totals;

                        Dts.Variables["User::order_no_list_demandware"].Value = order_no_list_demandware;
                        //MessageBox.Show(to_show);

                    }

                    //id_where_exception_occured = 10;
                    //10. set from_datetime as creation_date and to_datetime as last_modified for data filtered on order_no 
                    if (flag_field_to_filter_on == "O" && total_orders_count == "1")
                    {
                        string order_no_creation_datetime = original_document_xml.GetElementsByTagName("creation_date")[0].InnerText;
                        string order_no_lastmodified_datetime = original_document_xml.GetElementsByTagName("last_modified")[0].InnerText;

                        Dts.Variables["User::input_from_datetime"].Value = order_no_creation_datetime.Replace("T", " ").TrimEnd('Z') + "0000";
                        Dts.Variables["User::input_to_datetime"].Value = order_no_lastmodified_datetime.Replace("T", " ").TrimEnd('Z') + "0000";
                    }

                    //MessageBox.Show("Iteration number " + iteration_num + ". A total of " + Dts.Variables["User::ordersearch_hits_count"].Value.ToString() + " orders saved in a single xml file" + xmls_save_path + "\\XML_N" + iteration_num + ".xml");
                    if (iteration_num == 0) { Dts.Variables["User::total_orders_count"].Value = total_orders_count; } //Get total orders to be retrieved at the first response
                    Dts.Variables["User::iteration_number"].Value = iteration_num + 1; //Increment the iteration_number value

                    if((int)Dts.Variables["User::consecutive_error_counter_requests"].Value > 0)  { Dts.Variables["User::consecutive_error_counter_requests"].Value = 0; } //Set zero consecutive_error_counter_requests
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);

                //In case of error 500 (caused typically from too large responses) we decrement the number of order to be returned by a single request               
                if (ex.Message.Contains("500"))
                {
                    int ordersearch_hits_count_int = int.Parse(Dts.Variables["User::ordersearch_hits_count"].Value.ToString());
                    int orders_requested_count_int = int.Parse(Dts.Variables["User::orders_requested_count"].Value.ToString());
                    if (ordersearch_hits_count_int > 20 && orders_requested_count_int > 20) { 
                        Dts.Variables["User::ordersearch_hits_count"].Value = (ordersearch_hits_count_int - 20).ToString();
                        Dts.Variables["User::orders_requested_count"].Value = (orders_requested_count_int - 20).ToString();
                    }
                }
                else if (ex.Message.Contains("hexadecimal value"))
                {
                    Dts.Variables["flag_hexadecimal"].Value = true;
                }
                
                //Check where exception occurred
                string where_exception_occurred = "";
                switch (id_where_exception_occured)
                {
                    case -111:
                        where_exception_occurred = "start of package";
                        break;
                    case -1:
                        where_exception_occurred = "before the request";
                        break;
                    case 0:
                        where_exception_occurred = "before fixes, when starting to parse response";
                        break;
                    case 1:
                        where_exception_occurred = "c_orderStatusJSON to XML";
                        break;
                    case 2:
                        where_exception_occurred = "c_statusjson to XML";
                        break;
                    case 3:
                        where_exception_occurred = "tax fixes";
                        break;
                    case 4:
                        where_exception_occurred = "c_custom_item cut";
                        break;
                    case 5:
                        where_exception_occurred = "lineNumbers/shipmentList concatenation";
                        break;
                    case 6:
                        where_exception_occurred = "price_adjustment_id tagname change";
                        break;
                    case 7:
                        where_exception_occurred = "shipment_id tagname change";
                        break;
                    case 8:
                        where_exception_occurred = "employeeID and Email change";
                        break;
                    case 9:
                        where_exception_occurred = "order_no values saving";
                        break;
                    case 10:
                        
                        break;

                }
                Dts.Variables["User::where_exception_occurred_id"].Value = id_where_exception_occured;
                //MessageBox.Show(where_exception_occurred);

                //Pause execution so that next request will be delayed (ms computed according to number of consecutive errors)
                int ms = (10 + (int)Dts.Variables["User::consecutive_error_counter_requests"].Value * 10) * 1000;
                System.Threading.Thread.Sleep(ms);

                Dts.Variables["User::error_counter_requests"].Value = (int)Dts.Variables["User::error_counter_requests"].Value + 1; //Increment value of error_counter_requests
                Dts.Variables["User::consecutive_error_counter_requests"].Value = (int)Dts.Variables["User::consecutive_error_counter_requests"].Value + 1; //Increment value of consecutive_error_counter_requests

                //Add exception information in concatenated string
                var pattern = new Regex("[:!@#$%^&*()}{|\":?><\\[\\]\\;'/.,~]");
                string ex_Message = pattern.Replace(ex.Message, "");
                Dts.Variables["User::exception_message_concatenated"].Value = Dts.Variables["User::exception_message_concatenated"].Value.ToString() + "Iteration number: " + iteration_num + " - Exception: " + ex_Message + " - Occurred in " + where_exception_occurred + " - Additional info: " + additional_exception_info +";";

                //Check if there were 10 consecutive errors
                if ((int)Dts.Variables["User::consecutive_error_counter_requests"].Value == 10) {
                    Dts.Variables["User::flag_ten_consecutive_error"].Value = true;
                    //Dts.TaskResult = (int)ScriptResults.Failure;
                }
            }

            Dts.TaskResult = (int)ScriptResults.Success;
		}

        static bool IsAllDigits(string s)
        {
            foreach (char c in s)
            {
                if (!char.IsDigit(c))
                    return false;
            }
            return true;
        }

        static void checkNodes(XmlNode root, XmlNodeList nodeList, XmlDocument document, string ns)
        {

            foreach (XmlNode child in nodeList)
            {
                XmlNode child2 = child.FirstChild;
                if (child2 != null) //if (child.Name != "reason-code") 
                {
                    if (child2.Name == "#text")
                {
                    XmlNode temp_node = removeNamespace(child, document, ns);

                    //fix for fulfillment-date issue (missing semicolumn char)
                    if (temp_node.Name == "fulfillment-date")
                    {
                        string f_date = child2.InnerText; 
                        
                        if (f_date.Substring(f_date.Length - 3, 1) != ":")
                        {   //check if there is semi-column

                            string new_f_date = f_date.Substring(0, f_date.Length - 2) + ":" + f_date.Substring(f_date.Length - 2, 2);
                            temp_node.InnerText = new_f_date;
                        }
                    }

                    root.AppendChild(temp_node);
                }
                else
                {
                    XmlNode temp_node = document.CreateElement(child.Name, ns);
                    checkNodes(temp_node, child.ChildNodes, document, ns);
                    root.AppendChild(temp_node);
                }
                }
            }

        }


        static XmlNode removeNamespace(XmlNode node, XmlDocument document, string ns)
        {

            XmlNode return_node = document.CreateElement(node.Name, ns);
            return_node.InnerText = node.InnerText;
            return return_node;

        }

	}
}