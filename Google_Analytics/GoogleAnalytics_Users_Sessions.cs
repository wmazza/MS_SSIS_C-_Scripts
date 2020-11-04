using System;
using System.Linq;
using System.Data;
using Microsoft.SqlServer.Dts.Runtime;
using System.Windows.Forms;
using Google.Apis.Analytics.v3;
using Google.Apis.Auth.OAuth2;
using System.IO;
using Google.Apis.AnalyticsReporting.v4.Data;
using Google.Apis.AnalyticsReporting.v4;
using Google.Apis.Services;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Collections.Generic;

public class ScriptMain : UserComponent
{
    public override void PreExecute()
    {
        base.PreExecute();
        
    }
    
    public override void PostExecute()
    {
        base.PostExecute();
        
    }
	
    public override void CreateNewOutputRows()
    {
        string client_secret_file_path = this.Variables.googleclientsecretjsonpath; /
        string[] ViewIds = { this.Variables.googleviewIdUS, this.Variables.googleviewIdES }; // View IDs for different E-commerce sites
        int startdate_offset = this.Variables.startdateoffset;
        int enddate_offset = this.Variables.enddateoffset;
        string start_date = DateTime.Now.AddDays(-startdate_offset).ToString("yyyy-MM-dd");
        string end_date = DateTime.Now.AddDays(-enddate_offset).ToString("yyyy-MM-dd");
        
		for(int j = 0; j < 5; j++) {
            try
            {
                GoogleCredential credential = GetJsonCredential(client_secret_file_path);
                AnalyticsReportingService service = new AnalyticsReportingService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "GA API SESSIONS SSIS",
                });
                List<GetReportsResponse> responses = new List<GetReportsResponse>();
                foreach (string ViewId in ViewIds)
                {
                    var request = service.Reports.BatchGet(new GetReportsRequest
                    {
                        ReportRequests = new[] {
                                new ReportRequest{
                                    DateRanges = new[] { new DateRange{ StartDate = start_date, EndDate = end_date }}, //i.e. StartDate = "2020-04-27", EndDate = "2020-05-03"
                                    Dimensions = new[] { new Dimension{ Name = "ga:date" }},
                                    Metrics = new[] {new Metric{ Expression = "ga:users", Alias = "Users"}, new Metric{ Expression = "ga:sessions", Alias = "Sessions"}},
                                    ViewId = ViewId
                                }
                            }
                    });
                    var response = request.Execute();
                    responses.Add(response);
                }
                int num_rows = responses[0].Reports[0].Data.Rows.Count;
                for (int i = 0; i < num_rows; i++)
                {
                    this.GAUsersSessionsdataBuffer.AddRow();
                    string date_raw = responses[0].Reports[0].Data.Rows[i].Dimensions[0];
                    this.GAUsersSessionsdataBuffer.Date = date_raw.Substring(0, 4) + "-" + date_raw.Substring(4, 2) + "-" + date_raw.Substring(6, 2);
					
                    this.GAUsersSessionsdataBuffer.UsersUS = responses[0].Reports[0].Data.Rows[i].Metrics[0].Values[0];
                    this.GAUsersSessionsdataBuffer.SessionsUS = responses[0].Reports[0].Data.Rows[i].Metrics[0].Values[1];
                    
                    this.GAUsersSessionsdataBuffer.UsersES = responses[1].Reports[0].Data.Rows[i].Metrics[0].Values[0];
                    this.GAUsersSessionsdataBuffer.SessionsES = responses[1].Reports[0].Data.Rows[i].Metrics[0].Values[1];
                }
                break;
            }
            catch (Google.GoogleApiException ge)
            {
                int error_code = ge.Error.Code;
                this.Variables.exceptionmessage = ge.HttpStatusCode + " - " + ge.Error.Message;
                //MessageBox.Show(ge.HttpStatusCode + " - " + ge.Error.Message);
                int[] error_codes_to_not_break = { 429, 500, 503 };
                int[] error_codes_to_break = { 400, 401, 403 };
				
                //According to the error code the request is retried or not
                if (error_codes_to_break.Contains(error_code))
                {
                    break;
                }
                else if (error_codes_to_not_break.Contains(error_code))
                {
                    System.Threading.Thread.Sleep(1000 * j);
                }
            }
            catch (Exception ex)
            {
                this.Variables.exceptionmessage = ex.Message;
                //MessageBox.Show(ex.Message);
            }
        }
    }
	
    static GoogleCredential GetJsonCredential(string jsonFilePath)
    {
        GoogleCredential credential;
        using (Stream stream = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            credential = GoogleCredential.FromStream(stream);
        }
        string[] scopes = new string[] {
                    AnalyticsService.Scope.AnalyticsReadonly
                    };
        credential = credential.CreateScoped(scopes);
        return credential;
    }
}
