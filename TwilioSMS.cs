    public class TwilioSMS : SMSProvider
    {
        private static readonly string DefaultUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
        private const string url = "https://api.twilio.com/2010-04-01/Accounts/XXXXXXXXXX/Messages.json";
        #region 账户参数信息
        const string username = "XXXXXXXXXX";
        const string password = "XXXXXXX";
        const string fromnumber = "+170*******";
        #endregion
        
        public override NotificationResponse SendSMS(SMSRequest request)
        {
            var response = new NotificationResponse();

            try
            {
                var mobile = request.Mobile.StartsWith("+", StringComparison.Ordinal) ? request.Mobile : $"+86{request.Mobile}";

                IDictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("From", HttpUtility.UrlEncode(fromnumber));
                parameters.Add("Body", HttpUtility.UrlEncode(request.Message));
                parameters.Add("To", HttpUtility.UrlEncode(mobile));

                response.Message = PostHttpRequest(url, parameters);
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Error = ex.ToString();
            }

            return response;
        }

        #region Private Method
        private string GetAuthString()
        {
            string auth = $"{username}:{password}";

            byte[] bytes = Encoding.GetEncoding("utf-8").GetBytes(auth);

            return Convert.ToBase64String(bytes);
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受   
        }

        private string PostHttpRequest(string url, IDictionary<string, string> parameters)
        {
            HttpWebRequest request = null;
            //HTTPSQ请求
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            request = WebRequest.Create(url) as HttpWebRequest;
            request.ProtocolVersion = HttpVersion.Version10;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = DefaultUserAgent;

            //request.Headers = new WebHeaderCollection();
            request.Headers.Add("Authorization", $"Basic {GetAuthString()}");

            //如果需要POST数据   
            if (!(parameters == null || parameters.Count == 0))
            {
                var dataStr = StringFromateParameter(parameters);

                byte[] data = Encoding.UTF8.GetBytes(dataStr);

                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private string StringFromateParameter(IDictionary<string, string> parameters)
        {
            StringBuilder dataStr = new StringBuilder();
            foreach (string key in parameters.Keys)
            {
                dataStr.AppendFormat("{0}={1}&", key, parameters[key]);
            }

            return dataStr.ToString().TrimEnd('&');
        }

        #endregion
    }
