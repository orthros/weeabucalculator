using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class Log : StateBasedStorage<LogMessage>
    {

        /// <summary>
        /// All the messages since the beginning of time.
        /// </summary>
        public IEnumerable<LogMessage> MessageHistory
        { get { return GetHistory(); } }

        public string MessageHistoryString
        { get { return string.Join("\n", (from m in MessageHistory select m.ToString())); } }

        public Log(SimulationState currentState) : base(currentState)
        { }

        /// <summary>
        /// Write a log to this step
        /// </summary>
        /// <param name="time"></param>
        /// <param name="message"></param>
        public void Write(float time, string message)
        {
            AddValue(new LogMessage(time, message));
        }

        public void SaveLog(string logPath)
        {
            throw new NotImplementedException();
        }

        protected override StateBasedStorage<LogMessage> GetStorageFromStep(SimulationState step = null)
        {
            return (step ?? CurrentState).Log;
        }
    }

    public class LogMessage
    {
        public string message;
        public float time;

        public LogMessage(float time, string message)
        {
            this.message = message;
            this.time = time;
        }

        public LogMessage Clone()
        {
            return new LogMessage(time, message);
        }

        public override string ToString()
        {
            return $"{time:0.00} :: {message}";
        }
    }
}
