using ChatTranscriptService.com.salesforce.na15;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

namespace ChatTranscriptService
{
    public class SalesforceWrapper
    {
        private readonly string _user = "";
        private readonly string _password = "";
        
        private SforceService _binding;
        
        public SalesforceWrapper()
        {
            _user = ConfigurationManager.AppSettings["salesforceUser"];
            _password = ConfigurationManager.AppSettings["salesforcePassword"];
        }
        
        public void Connect()
        {
            _binding = new SforceService();

            // Time out after a minute
            _binding.Timeout = 60000;

            LoginResult loginResult = null;

            loginResult = _binding.login(_user, _password);

            //Change the binding to the new endpoint
            _binding.Url = loginResult.serverUrl;

            //Create a new session header object and set the session id to that returned by the login
            _binding.SessionHeaderValue = new SessionHeader();
            _binding.SessionHeaderValue.sessionId = loginResult.sessionId;
        }

        public void RetrieveActivitySettings(string callIdKey, out string activityId, out string ownerId)
        {
            activityId = string.Empty;
            ownerId = string.Empty;

            QueryResult queryResult = _binding.query("SELECT Id, OwnerId FROM Task where CallObject='" + callIdKey + "'");

            sObject[] tasks = queryResult.records;
            if (tasks != null && tasks.Length == 1)
            {
                Task task = (Task)tasks[0];
                activityId = task.Id;
                ownerId = task.OwnerId;
            }

            throw new Exception("A task for call Id " + callIdKey + " was not found");
        }
        
        public void AttachToActivity(string filename, string activityId, string ownerId)
        {
            Attachment attachment = new Attachment();

            attachment.Body = ReadByteArrayFromFile(filename);

            attachment.ParentId = activityId;
            attachment.Name = "Chat Transcript";
            attachment.ContentType = "text/html";

            if (!String.IsNullOrEmpty(ownerId))
            {
                attachment.OwnerId = ownerId;
            }            
            sObject[] attachments = new sObject[1];
            attachments[0] = attachment;

            //create the object(s) by sending the array to the web service
            SaveResult[] attachmentSaveResult = _binding.create(attachments);

            for (int j = 0; j < attachmentSaveResult.Length; j++)
            {
                if (attachmentSaveResult[j].success)
                {
                    Console.WriteLine("An attachment was create with an id of: "
                        + attachmentSaveResult[j].id);
                }
                else
                {
                    //there were errors during the create call, go through the errors
                    //array and write them to the screen
                    for (int i = 0; i < attachmentSaveResult[i].errors.Length; i++)
                    {
                        //get the next error
                        Error err = attachmentSaveResult[i].errors[i];
                        Console.WriteLine("Errors were found on item " + i.ToString());
                        Console.WriteLine("Error code is: " + err.statusCode.ToString());
                        Console.WriteLine("Error message: " + err.message);
                    }
                }
            }
        }

        public static byte[] ReadByteArrayFromFile(string fileName)        {            byte[] buff = null;            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))            {                BinaryReader br = new BinaryReader(fs);                long numBytes = new FileInfo(fileName).Length;                buff = br.ReadBytes((int)numBytes);                return buff;            }        }

    }
}