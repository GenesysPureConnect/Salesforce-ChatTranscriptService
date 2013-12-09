using ININ.IceLib.Connection;
using ININ.IceLib.QualityManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ChatTranscriptService
{
    public class TranscriptService : ITranscriptService
    {
        public void UploadChatTranscript(string callIdKey, string recordingId, bool async)
        {
            if (async)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
                {
                    try
                    {
                        UploadChatTranscriptImpl(callIdKey, recordingId);
                    }
                    catch(Exception ex)
                    {
                        ININ.IceLib.Tracing.TraceException(ex, "Exception caught uploading transcript");
                    }
                }), null);
            }
            else
            {
                UploadChatTranscriptImpl(callIdKey, recordingId);
            }
        }

        public void UploadChatTranscriptImpl(string callIdKey, string recordingId)
        {
            var cic = new CicWrapper();
            cic.Connect();

            try
            {
                var fileName = cic.DownloadTranscript(recordingId);

                if (String.IsNullOrEmpty(fileName))
                {
                    return;
                }

                var salesforce = new SalesforceWrapper();

                string owner = null;
                string activityId = null;

                salesforce.RetrieveActivitySettings(callIdKey, out activityId, out owner);
                salesforce.AttachToActivity(fileName, activityId, owner);

             
            }
            finally
            {
                cic.Disconnect();
            }
        }
    }
}
