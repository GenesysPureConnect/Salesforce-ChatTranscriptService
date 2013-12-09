using ININ.IceLib.Connection;
using ININ.IceLib.QualityManagement;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace ChatTranscriptService
{
    public class CicWrapper
    {
        private string _host = "";
        private string _icUser = "";
        private string _icPassword = "";
        private Session _session = null;

        public CicWrapper()
        {
            _icUser = ConfigurationManager.AppSettings["cicUser"];
            _icPassword = ConfigurationManager.AppSettings["cicPassword"];
            _host = ConfigurationManager.AppSettings["cicServer"];
        }

        public string DownloadTranscript(string recordingId)
        {
            RecordingsManager recordingManager = QualityManagementManager.GetInstance(_session).RecordingsManager;
            Uri downloadURI = recordingManager.GetExportUri(recordingId, RecordingMediaType.PrimaryMedia, "", 0);

            if (downloadURI == null)
            {
                return String.Empty;
            }

            WebClient webClient = new WebClient();
            var downloadPath = Path.Combine(Path.GetTempPath(), String.Format("{0}.html", recordingId));
            webClient.DownloadFile(downloadURI, downloadPath);
            FormatFile(downloadPath);

            return downloadPath;
        }

        /// <summary>
        /// This method takes the transcript and creates a html document to format it nicely. 
        /// </summary>
        /// <param name="path"></param>
        private void FormatFile(string path)
        {
            if (!File.Exists(path)) return;

            string text = String.Empty;
            using (TextReader tr = new StreamReader(path))
            {
                text = tr.ReadToEnd();
            }

            string[] lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            List<string> replacement = new List<string>();
            string header = @"<html><head><style type='text/css'>span.From { color: Gray;	} ul { margin-top: 0; margin-bottom: 0; } </style>";
            string lineformat = @"<div><span class='From'>{0}</span><ul><li>{1}</li></ul></div>";
            string footer = @"</body></html>";

            replacement.Add(header);

            foreach (string line in lines)
            {
                string clean = String.Empty;
                clean = Regex.Replace(line, @"\.{1}.+\]: {1}", delegate(Match match)
                {
                    return "]:|||";
                });

                string[] strings = clean.Split(new string[] { "|||" }, StringSplitOptions.None);

                if (strings.Length == 2)
                {
                    replacement.Add(String.Format(lineformat, strings[0], strings[1]));
                }
            }

            replacement.Add(footer);

            using (TextWriter tw = new StreamWriter(path))
            {
                tw.Write(string.Join(String.Empty, replacement.ToArray()));
            }
        }

        public void Connect()
        {
            _session = new Session();
            _session.Connect(new SessionSettings(),
                            new HostSettings(new HostEndpoint(_host)),
                            new ICAuthSettings(_icUser, _icPassword),
                            new StationlessSettings());

            if (_session.ConnectionState != ConnectionState.Up)
            {
                throw new Exception("Unable to connect to server: " + _session.ConnectionStateMessage);
            }
        }
            
        public void Disconnect()
        {
            if (_session != null && _session.ConnectionState == ConnectionState.Up)
            {
                _session.Disconnect();
            }
        }
    }


}