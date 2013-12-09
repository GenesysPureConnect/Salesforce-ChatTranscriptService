using System.ServiceModel;

namespace ChatTranscriptService
{
    [ServiceContract]
    public interface ITranscriptService
    {
        [OperationContract]
        void UploadChatTranscript(string callIdKey, string recordingId, bool async);
    }
}
