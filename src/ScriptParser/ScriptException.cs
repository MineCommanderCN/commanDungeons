using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ShenGu.Script
{
    public abstract class ScriptException : Exception
    {
        public ScriptException() { }
        public ScriptException(string message) : base(message) { }
        public ScriptException(string message, Exception innerException) : base(message, innerException) { }
        protected ScriptException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class ScriptParseException : ScriptException
    {
        private string script;
        private int charIndex;
        private int lineIndex, columnIndex;

        public ScriptParseException(string script, int charIndex, string errorMessage) : base(errorMessage)
        {
            this.script = script;
            this.charIndex = charIndex;
            this.lineIndex = this.columnIndex = -1;
        }
        public ScriptParseException(string message) : base(message) { }
        public ScriptParseException(string message, Exception innerException) : base(message, innerException) { }
        protected ScriptParseException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        
        private void CheckLine()
        {
            if (lineIndex < 0 && charIndex >= 0 && !string.IsNullOrEmpty(script))
            {
                if (charIndex == 0)
                {
                    columnIndex = 0;
                    lineIndex = 0;
                }
                else
                {
                    int lineCounter = 0, index, lastIndex = 0;
                    do
                    {
                        index = script.IndexOf("\n", lastIndex);
                        if (index < 0 || charIndex <= index) break;
                        lineCounter++;
                        lastIndex = ++index;
                    } while (true);
                    columnIndex = charIndex - lastIndex;
                    lineIndex = lineCounter;
                }
            }
        }

        public string Script { get { return script; } }
        public int CharIndex { get { return charIndex; } }
        public int LineIndex { get { CheckLine(); return lineIndex; } }
        public int ColumnIndex { get { CheckLine(); return columnIndex; } }
    }

    public interface IScriptException
    {
        IScriptObject Value { get; }
    }

    public class ScriptExecuteException : ScriptException, IScriptException
    {
        private IScriptObject e;

        public ScriptExecuteException(IScriptObject e) { this.e = e; }
        public ScriptExecuteException(string message) : base(message) { }
        public ScriptExecuteException(string message, Exception innerException) : base(message, innerException) { }
        protected ScriptExecuteException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public IScriptObject Value
        {
            get
            {
                if (e == null)
                    e = ScriptString.Create(this.Message);
                return e;
            }
        }
    }
}
