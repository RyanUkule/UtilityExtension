    public class AliyunEmailSender : EmailProvider
    {
        private const string _emailHost = "https://dm.aliyuncs.com/";
        private const string _emailVersion = "2015-11-23";
        private const string _signatureVersion = "1.0";
        private const string _signatureMethod = "HMAC-SHA1";
        private const string _httpMethod = "POST";

        #region Public Method
        public override NotificationResponse SendEmail(EmailRequest request)
        {
            var response = new NotificationResponse();

            try
            {
                var parameters = new Dictionary<string, string>();

                parameters.Add("AccountName", AccountName);
                parameters.Add("AddressType", "1");
                parameters.Add("ReplyToAddress", "True");
                parameters.Add("Subject", request.Subject);
                parameters.Add("ToAddress", request.ToAddress);
                parameters.Add("Action", "SingleSendMail");
                parameters.Add("TagName", request.TagName);
                parameters.Add("FromAlias", FromAlias);

                if (request.IsHtmlBody)
                    parameters.Add("HtmlBody", request.Message);
                else
                    parameters.Add("TextBody", request.Message);

                AddCommonParameters(parameters);
                parameters = parameters.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

                string signature = GetSignature(parameters);

                parameters.Add("Signature", signature);
                parameters = parameters.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

                var url = $"{_emailHost}?{FormateUrl(parameters, true)}";
                var result = HttpPost(url, null);

                var emailResponse = JsonConvert.DeserializeObject<EmailResponse>(result);
                response.Success = emailResponse != null && !string.IsNullOrEmpty(emailResponse.RequestId);
                response.Message = result;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Error = ex.ToString();
            }

            return response;
        }

        #endregion

        #region Private Method
        private void AddCommonParameters(Dictionary<string, string> parameters)
        {
            parameters.Add("Format", "JSON");
            parameters.Add("Timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            parameters.Add("SignatureNonce", Guid.NewGuid().ToString());

            parameters.Add("Version", _emailVersion);
            parameters.Add("AccessKeyId", AccessKey);
            parameters.Add("SignatureMethod", _signatureMethod);
            parameters.Add("SignatureVersion", _signatureVersion);
            parameters.Add("RegionId", RegionId);
        }

        private string FormateUrl(Dictionary<string, string> parameter, bool urlEncode)
        {
            var result = new StringBuilder();

            foreach (var pair in parameter)
            {
                var key = urlEncode ? UrlEncode(pair.Key) : pair.Key;
                var value = urlEncode ? UrlEncode(pair.Value) : pair.Value;

                result.Append($"{key}={value}&");
            }

            return result.ToString().TrimEnd('&');
        }

        private string GetSignature(Dictionary<string, string> parameters)
        {
            var parameterString = FormateUrl(parameters, true);

            var stringToSign = $"{_httpMethod}&{UrlEncode("/")}&{UrlEncode(parameterString)}";
            var signature = CryptoProviders.HMACSHA1(stringToSign, $"{Secretkey}&");

            return signature;
        }

        private string UrlEncode(string str)
        {
            if (str == null)
                return null;

            StringBuilder builder = new StringBuilder();
            foreach (char c in str)
            {
                var tmp = HttpUtility.UrlEncode(c.ToString(), Encoding.UTF8);
                
                tmp = ReplaceCode(tmp.ToUpper());

                if (tmp.Length > 1)
                {
                    builder.Append(tmp);
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private string ReplaceCode(string message)
        {
            var dic = new Dictionary<string, string>();
            dic.Add("+", "%20");
            dic.Add("*", "%2A");
            dic.Add("%7E", "~");
            dic.Add("(", "%28");
            dic.Add(")", "%29");
            dic.Add("!", "%21");

            foreach (var pair in dic)
            {
                message = message.Replace(pair.Key, pair.Value);
            }

            return message;
        }

        private static string HttpPost(string Url, string postDataStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            if (postDataStr != null)
            {
                byte[] btBodys = Encoding.UTF8.GetBytes(postDataStr);
                request.ContentLength = btBodys.Length;
                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(btBodys, 0, btBodys.Length);
                }
            }

            var result = string.Empty;

            using (HttpWebResponse httpWebResponse = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }

                request.Abort();
            }

            return result;
        }

        private static string HttpGet(string Url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string encoding = response.ContentEncoding;
            if (encoding == null || encoding.Length < 1)
            {
                encoding = "UTF-8"; //默认编码  
            }
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding));
            string retString = reader.ReadToEnd();
            return retString;
        }
        #endregion

    }
