namespace ConsoleApp1{
    using System;
    using System.Text;
    using Eventuous;

    public class EventSerializer : IEventSerializer{
        #region Constructors

        public EventSerializer(){
            this.ContentType = "application/json";
        }

        #endregion

        #region Properties

        public String ContentType{ get; }

        #endregion

        #region Methods

        public Object? DeserializeEvent(ReadOnlySpan<Byte> data, String eventType){
            return new TopLevel.Event(Encoding.Default.GetString(data), eventType);
        }

        public (String EventType, Byte[] Payload) SerializeEvent(Object evt){
            throw new NotImplementedException();
        }

        public Byte[] SerializeMetadata(Metadata evt){
            throw new NotImplementedException();
        }

        #endregion
    }
}