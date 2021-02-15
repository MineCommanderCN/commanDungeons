using System;
using System.Collections.Generic;
using System.Text;

namespace ShenGu.Script
{
    public class TagParser
    {
        public const string DefaultOutputFuncName = "__out", DefaultWriteFuncName = "__write";
        public const string KEY_Executor = "@@Executor";
        public const string KEY_ComponentName = "Component";
        private readonly static Dictionary<string, ParseNodeCallback> dicSysNodeParsers;
        private IScriptBuilder builder;
        private string text;
        private int index, offset, length;
        private Dictionary<string, ParseNodeCallback> dicNodeParsers, dicTempNodeParsers;
        private TagNodeInfo stack, current;
        private readonly static CheckChildCallback switchChildCheck = new CheckChildCallback(CheckSwitchChildren);
        private readonly static ParseNodeCallback componentTagParse = new ParseNodeCallback(ParseComponentTag);

        static TagParser()
        {
            dicSysNodeParsers = new Dictionary<string, ParseNodeCallback>(StringComparer.OrdinalIgnoreCase);
            dicSysNodeParsers.Add("if", new ParseNodeCallback(ParseIf));
            dicSysNodeParsers.Add("else", new ParseNodeCallback(ParseElse));
            dicSysNodeParsers.Add("for", new ParseNodeCallback(ParseFor));
            dicSysNodeParsers.Add("while", new ParseNodeCallback(ParseWhile));
            dicSysNodeParsers.Add("do", new ParseNodeCallback(ParseDoWhile));
            dicSysNodeParsers.Add("switch", new ParseNodeCallback(ParseSwitch));
            dicSysNodeParsers.Add("case", new ParseNodeCallback(ParseCase));
            dicSysNodeParsers.Add("default", new ParseNodeCallback(ParseDefault));
            dicSysNodeParsers.Add("break", new ParseNodeCallback(ParseBreak));
            dicSysNodeParsers.Add("continue", new ParseNodeCallback(ParseContinue));
            dicSysNodeParsers.Add("line", new ParseNodeCallback(ParseLine));
            dicSysNodeParsers.Add(string.Empty, new ParseNodeCallback(ParseCode));
            dicSysNodeParsers.Add("code", new ParseNodeCallback(ParseCode));
            dicSysNodeParsers.Add("try", new ParseNodeCallback(ParseTry));
            dicSysNodeParsers.Add("catch", new ParseNodeCallback(ParseCatch));
            dicSysNodeParsers.Add("finally", new ParseNodeCallback(ParseFinally));
            dicSysNodeParsers.Add("export", new ParseNodeCallback(ParseExport));
            dicSysNodeParsers.Add("import", new ParseNodeCallback(ParseImport));
            dicSysNodeParsers.Add("refer", new ParseNodeCallback(ParseRefer));
            dicSysNodeParsers.Add("debugger", new ParseNodeCallback(ParseDebugger));
        }

        #region 节点处理方法

        private static void ParseDebugger(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element || info.NodeType == TagNodeType.WholeElement)
                builder.AppendBodyScript("\r\ndebugger;\r\n");
        }

        private static string CombineArgusObject(Dictionary<string, string> dic)
        {
            StringBuilder sbArgus = null;
            if (dic != null)
            {
                foreach (KeyValuePair<string, string> kv in dic)
                {
                    string key = kv.Key;
                    if (key.Length > 1 && key[0] == ':')
                    {
                        if (sbArgus == null)
                        {
                            sbArgus = new StringBuilder();
                            sbArgus.Append('{');
                        }
                        else sbArgus.Append(',');
                        sbArgus.Append(key.Substring(1));
                        sbArgus.Append(':');
                        sbArgus.Append(kv.Value);
                    }
                }
                if (sbArgus != null) sbArgus.Append('}');
            }
            return sbArgus != null ? sbArgus.ToString() : string.Empty;
        }

        private static void ParseRefer(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element || info.NodeType == TagNodeType.WholeElement)
            {
                string src = CheckGetAttrValue(parser, info, "refer", "src");
                builder.CheckComponent(src);
                builder.AppendBodyScript(string.Format("\r\nnew {0}('{1}').execute({2});\r\n", KEY_ComponentName, src, CombineArgusObject(info.Attrs)));
            }
        }

        private static void ParseImport(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element || info.NodeType == TagNodeType.WholeElement)
            {
                string tag = CheckGetAttrValue(parser, info, "import", "tag");
                string src = CheckGetAttrValue(parser, info, "import", "src");
                builder.CheckComponent(src);
                builder.AppendBodyScript(string.Format("\r\nvar __c_{0} = new {1}('{2}');\r\n", tag, KEY_ComponentName, src));
                parser.RegisterTempNode(tag, componentTagParse);
            }
        }

        private static void ParseComponentTag(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            builder.AppendBodyScript(string.Format("\r\n__c_{0}.execute({1});\r\n", info.Name, CombineArgusObject(info.Attrs)));
        }

        private static void ParseExport(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            string script = parser.ReadNodeContent();
            if (script != null) script = script.Trim();
            if (!string.IsNullOrEmpty(script))
                builder.ArgusScript = script;
        }

        private static void AppendBrace(IScriptBuilder builder, bool isStarted)
        {
            builder.AppendBodyScript(isStarted ? "\r\n{\r\n" : "\r\n}\r\n");
        }

        private static void ThrowAttrNotExist(TagParser parser, string tagName, string attrName)
        {
            parser.ThrowError("“{0}”节点必须包含“{1}”属性。", tagName, attrName);
        }

        private static string CheckGetAttrValue(TagParser parser, TagNodeInfo info, string tagName, string attrName)
        {
            string code = info.GetAttrValue(attrName);
            if (string.IsNullOrEmpty(code))
                ThrowAttrNotExist(parser, tagName, attrName);
            return code;
        }

        private static void ParseIf(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element)
            {
                string code = CheckGetAttrValue(parser, info, "if", "code");
                builder.AppendBodyScript(string.Format("if ({0})", code));
                AppendBrace(builder, true);
            }
            else if (info.NodeType == TagNodeType.EndElement)
                AppendBrace(builder, false);
        }

        private static void ParseElse(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element)
            {
                TagNodeInfo prev = info.Previous;
                if (prev == null
                    || (prev.Name != "if" && prev.Name != "else")
                    || (prev.Name == "else" && prev.GetAttrValue("if") == null))
                    parser.ThrowError("“else”节点必须在“if”或“else if”节点之后。");
                string code = info.GetAttrValue("if");
                if (code == null)
                    builder.AppendBodyScript("else");
                else
                    builder.AppendBodyScript(string.Format("else if ({0})", code));
                AppendBrace(builder, true);
            }
            else
                AppendBrace(builder, false);
        }

        private static void ParseFor(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element)
            {
                string code = CheckGetAttrValue(parser, info, "for", "code");
                builder.AppendBodyScript(string.Format("for ({0})", code));
                AppendBrace(builder, true);
            }
            else if (info.NodeType == TagNodeType.EndElement)
                AppendBrace(builder, false);
        }

        private static void ParseWhile(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            string code = CheckGetAttrValue(parser, info, "while", "code");
            if (info.NodeType == TagNodeType.Element)
            {
                builder.AppendBodyScript(string.Format("while ({0})", code));
                AppendBrace(builder, true);
            }
            else if (info.NodeType == TagNodeType.EndElement)
                AppendBrace(builder, false);
        }

        private static void ParseDoWhile(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element)
            {
                builder.AppendBodyScript("do");
                AppendBrace(builder, true);
            }
            else if (info.NodeType == TagNodeType.EndElement)
            {
                string code = CheckGetAttrValue(parser, info, "do...while", "while");
                AppendBrace(builder, false);
                builder.AppendBodyScript(string.Format("while({0});\r\n", code));
            }
        }

        private static void ParseSwitch(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element)
            {
                string code = CheckGetAttrValue(parser, info, "switch", "code");
                builder.AppendBodyScript(string.Format("switch ({0})", code));
                AppendBrace(builder, true);
                info.CheckChildHandler = switchChildCheck;
            }
            else if (info.NodeType == TagNodeType.EndElement)
                AppendBrace(builder, false);
        }

        private static void CheckSwitchChildren(TagParser parser, IScriptBuilder builder, TagNodeInfo parent, TagNodeInfo child)
        {
            switch(child.NodeType)
            {
                case TagNodeType.Element:
                case TagNodeType.EndElement:
                case TagNodeType.WholeElement:
                    if (child.Name == "case" || child.Name == "default") return;
                    break;
            }
            parser.ThrowError("“switch”的一级子节点，必须是“case”节点");
        }

        private static void ParseCase(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.Parent == null || info.Parent.NodeType != TagNodeType.Element || info.Parent.Name != "switch")
                parser.ThrowError("“case”必须包含在“switch”内部。");
            switch (info.NodeType)
            {
                case TagNodeType.Element:
                    {
                        string code = CheckGetAttrValue(parser, info, "case", "code");
                        builder.AppendBodyScript(string.Format("case {0}:", code));
                        AppendBrace(builder, true);
                        break;
                    }
                case TagNodeType.EndElement:
                    {
                        string str = info.GetAttrValue("ignoreBreak");
                        if (str == null || str.Trim() != "true")
                            builder.AppendBodyScript("break;");
                        AppendBrace(builder, false);
                        break;
                    }
                case TagNodeType.WholeElement:
                    {
                        string code = CheckGetAttrValue(parser, info, "case", "code");
                        builder.AppendBodyScript(string.Format("case {0}:\r\n", code));
                        break;
                    }
            }
        }

        private static void ParseDefault(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element)
            {
                if (info.Parent == null || info.Parent.NodeType != TagNodeType.Element || info.Parent.Name != "switch")
                    parser.ThrowError("“default”必须包含在“switch”内部。");
                builder.AppendBodyScript("default:");
                AppendBrace(builder, true);
            }
            else if (info.NodeType == TagNodeType.EndElement)
            {
                string str = info.GetAttrValue("ignoreBreak");
                if (str == null || str.Trim() != "true")
                    builder.AppendBodyScript("break;");
                AppendBrace(builder, false);
            }
        }

        private static void ParseBreak(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element || info.NodeType == TagNodeType.WholeElement)
                builder.AppendBodyScript("break;\r\n");
        }

        private static void ParseContinue(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element || info.NodeType == TagNodeType.WholeElement)
                builder.AppendBodyScript("continue;\r\n");
        }

        private static void ParseLine(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element || info.NodeType == TagNodeType.WholeElement)
            {
                string code = CheckGetAttrValue(parser, info, "line", "code");
                builder.AppendBodyScript(code);
                if (!code.EndsWith(";")) builder.AppendBodyScript(";\r\n");
                else builder.AppendBodyScript("\r\n");
            }
        }

        private static void ParseCode(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element)
            {
                string content = parser.ReadNodeContent();
                builder.AppendBodyScript(content);
                builder.AppendBodyScript("\r\n");
            }
        }

        private static void ParseTry(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element)
            {
                builder.AppendBodyScript("try");
                AppendBrace(builder, true);
            }
            else if (info.NodeType == TagNodeType.EndElement)
                AppendBrace(builder, false);
        }

        private static void ParseCatch(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element)
            {
                if (info.Previous == null || info.Parent.Name != "try")
                    parser.ThrowError("“catch”必须在“try”之后。");
                string varName = CheckGetAttrValue(parser, info, "catch", "var");
                builder.AppendBodyScript(string.Format("catch ({0})", varName));
                AppendBrace(builder, true);
            }
            else if (info.NodeType == TagNodeType.EndElement)
                AppendBrace(builder, false);
        }

        private static void ParseFinally(TagParser parser, IScriptBuilder builder, TagNodeInfo info)
        {
            if (info.NodeType == TagNodeType.Element)
            {
                if (info.Previous == null || (info.Parent.Name != "try" && info.Parent.Name != "catch"))
                    parser.ThrowError("“catch”必须在“try”或者“catch”之后。");
                builder.AppendBodyScript("finally");
                AppendBrace(builder, true);
            }
            else if (info.NodeType == TagNodeType.EndElement)
                AppendBrace(builder, false);
        }

        #endregion

        #region 私有方法

        private static bool IsWhiteSpace(char ch)
        {
            switch(ch)
            {
                case ' ':
                case '\t':
                case 'r':
                case '\n':
                    return true;
            }
            return false;
        }

        private static bool IsNameChar(char ch)
        {
            return (ch >= 'a' && ch <= 'z')
                || (ch >= 'A' && ch <= 'Z')
                || ch == '-' || ch == '_' || ch == ':';
        }

        private void SetCurrent(TagNodeInfo info)
        {
            if (stack != null)
            {
                CheckChildCallback check = stack.CheckChildHandler;
                if (check != null) check(this, builder, stack, info);
            }
            info.Previous = current;
            info.Parent = stack;
            current = info;
        }

        private void PushCurrent(TagNodeInfo info)
        {
            if (stack != null)
            {
                CheckChildCallback check = stack.CheckChildHandler;
                if (check != null) check(this, builder, stack, info);
            }
            info.Previous = current;
            info.Parent = stack;
            current = null;
            stack = info;
        }

        private TagNodeInfo PopCurrent(string tagName)
        {
            if (stack != null && stack.NodeType == TagNodeType.Element && stack.Name == tagName)
            {
                stack.NodeType = TagNodeType.EndElement;
                current = stack;
                stack = stack.Parent;
                return current;
            }
            ThrowError("读取到不匹配的结束节点：" + tagName);
            return null;
        }

        private void ThrowError(string errorMsg, params object[] tempateArgus)
        {
            if (tempateArgus != null && tempateArgus.Length > 0) errorMsg = string.Format(errorMsg, tempateArgus);
            throw new Exception(errorMsg);
        }

        private int LocateName(int i)
        {
            for (int j = i; j < length; j++)
            {
                if (!IsNameChar(text[j])) return j - i;
            }
            return length - i;
        }

        private string ReadName(int i, int len)
        {
            return len == 0 ? string.Empty : text.Substring(i, len);
        }

        private int LocateStringValue(int i)
        {
            char ch = text[i];
            if (ch == '\'' || ch == '"')
            {
                for(int j = i + 1; j < length; j++)
                {
                    if (ch == text[j]) return j - i + 1;
                }
            }
            return -1;
        }

        private string ReadStringValue(int i, int len)
        {
            if (len == 2) return string.Empty;
            return text.Substring(i + 1, len - 2);
        }

        private void SkipWhiteSpace()
        {
            int len = text.Length;
            for(; offset < len; offset++)
            {
                if (!IsWhiteSpace(text[offset])) break;
            }
        }

        private void ParseText(bool isWhiteSpace)
        {
            if (index < offset)
            {
                if (!isWhiteSpace)
                {
                    TagNodeInfo info = new TagNodeInfo(TagNodeType.Text, null);
                    info.Index = index;
                    info.Length = offset - index;
                    SetCurrent(info);
                    builder.AppendBodyScript(string.Format("{0}({1}, {2});\r\n", OutputFunctionName, info.Index, info.Length));
                }
                index = offset;
            }
        }

        private void ParseNode(bool isEnd)
        {
            int nameLen = LocateName(offset);
            string name = ReadName(offset, nameLen);
            offset += nameLen;

            TagNodeInfo info = null;
            char ch;
            do
            {
                SkipWhiteSpace();
                if (offset >= length) ThrowError("节点“{0}”没有结束符", name);
                ch = text[offset];
                if (ch == '/')
                {
                    if (++offset >= length || text[offset] != '>' || isEnd) ThrowError("节点“{0}”中读取到错误的符号：{1}", name, ch);
                    offset++;
                    if (info == null)
                        info = new TagNodeInfo(TagNodeType.WholeElement, name);
                    else
                        info.NodeType = TagNodeType.WholeElement;
                    SetCurrent(info);
                    break;
                }
                else if (ch == '>')
                {
                    offset++;
                    if (isEnd)
                    {
                        if (info != null) ThrowError("结束节点“{0}”不允许存在属性", name);
                        info = PopCurrent(name);
                        break;
                    }
                    if (info == null)
                        info = new TagNodeInfo(TagNodeType.Element, name);
                    PushCurrent(info);
                    break;
                }
                int attrLen = LocateName(offset);
                if (attrLen == 0) ThrowError("节点“{0}”的属性中，存在特殊字符：{1}", name, ch);
                string attrName = ReadName(offset, attrLen);
                offset += attrLen;
                SkipWhiteSpace();
                if (offset >= length || text[offset] != '=') ThrowError("节点“{0}”读取不到属性“{1}”的值", name, attrName);
                offset++;
                SkipWhiteSpace();
                attrLen = LocateStringValue(offset);
                if (attrLen < 0) ThrowError("节点“{0}”属性“{1}”的值，必须以单引号或双引号开始。", name, attrName);
                string attrValue = ReadStringValue(offset, attrLen);
                offset += attrLen;
                if (info == null)
                    info = new TagNodeInfo(TagNodeType.Element, name);
                info.Attrs.Add(attrName, attrValue);
            } while (true);

            ParseNodeCallback callback;
            if ((dicTempNodeParsers != null && dicTempNodeParsers.TryGetValue(info.Name, out callback))
                || (dicNodeParsers != null && dicNodeParsers.TryGetValue(info.Name, out callback))
                || dicSysNodeParsers.TryGetValue(info.Name, out callback))
                callback(this, builder, info);
            index = offset;
        }

        private void ParseLine()
        {
            int firstIndex = offset;
            for(;offset < length; offset++)
            {
                char ch = text[offset];
                if (ch == '}')
                {
                    string script = ReadName(firstIndex, offset - firstIndex);
                    builder.AppendBodyScript(string.Format("{0}({1});\r\n", WriteFunctionName, script));
                    offset++;
                    break;
                }
            }
            index = offset;
        }

        private void RegisterTempNode(string tagName, ParseNodeCallback callback)
        {
            if (dicTempNodeParsers == null) dicTempNodeParsers = new Dictionary<string, ParseNodeCallback>(StringComparer.OrdinalIgnoreCase);
            dicTempNodeParsers[tagName] = callback;
        }

        #endregion

        #region 重载方法
        
        protected virtual string OutputFunctionName { get { return DefaultOutputFuncName; } }

        protected virtual string WriteFunctionName { get { return DefaultWriteFuncName; } }

        protected virtual Type ComponentObjectType { get { return typeof(ScriptComponentObject); } }

        #endregion

        #region 公共方法

        public void Register(string tagName, ParseNodeCallback callback)
        {
            if (dicNodeParsers == null) dicNodeParsers = new Dictionary<string, ParseNodeCallback>(StringComparer.OrdinalIgnoreCase);
            dicNodeParsers.Add(tagName, callback);
        }

        private void InternalParse(IScriptBuilder builder, string script)
        {
            this.builder = builder;
            this.text = script;

            index = 0;
            length = text.Length;
            int len = text.Length - 2;
            bool isWhitespace = true;
            for (offset = 0; offset < len; )
            {
                char ch = text[offset];
                if (ch == '<')
                {
                    char ch1 = text[offset + 1];
                    if (ch1 == '@')
                    {
                        ParseText(isWhitespace);
                        isWhitespace = true;
                        offset += 2;
                        ParseNode(false);
                        continue;
                    }
                    else if (ch1 == '/' && text[offset + 2] == '@')
                    {
                        ParseText(isWhitespace);
                        isWhitespace = true;
                        offset += 3;
                        ParseNode(true);
                        continue;
                    }
                }
                else if (ch == '@' && text[offset + 1] == '{')
                {
                    ParseText(isWhitespace);
                    isWhitespace = true;
                    offset += 2;
                    ParseLine();
                    continue;
                }
                if (!IsWhiteSpace(ch)) isWhitespace = false;
                offset++;
            }
            offset = length;
            ParseText(isWhitespace);
        }

        public void Parse(IScriptBuilder builder, string script, bool doCheck)
        {
            dicTempNodeParsers = null;
            InternalParse(builder, script);
            if (doCheck)
            {
                string str = builder.BodyScript;
                ScriptParser.Parse(str);
                str = builder.ArgusScript;
                if (!string.IsNullOrEmpty(str))
                    ScriptParser.Parse(str);
            }
        }

        public string Parse(string script, bool doCheck)
        {
            StringScriptBuilder builder = new StringScriptBuilder();
            Parse(builder, script, doCheck);
            return builder.ToString();
        }

        public void Execute(ScriptParser parser, ScriptContext context, IScriptExecutor executor)
        {
            context.SetCacheValue(KEY_Executor, executor);
            context.AddValue(OutputFunctionName, new OutputFunctionCallback(executor.Output));
            context.AddValue(WriteFunctionName, new WriteFunctionCallback(executor.Write));
            context.AddValue(KEY_ComponentName, ComponentObjectType);
            parser.Execute(context);
        }

        public void Execute(string script, ScriptContext context, IScriptExecutor executor)
        {
            ScriptParser parser = ScriptParser.Parse(script);
            Execute(parser, context, executor);
        }

        public string ReadNodeContent()
        {
            TagNodeInfo info = stack;
            if (info != null)
            {
                int len = length - 5;
                int firstIndex = offset, lastIndex = offset;
                for (; offset < len; offset++)
                {
                    char ch = text[offset];
                    if (ch == '<' && text[offset + 1] == '/' && text[offset + 2] == '@')
                    {
                        lastIndex = offset;
                        offset += 3;
                        int nameLen = LocateName(offset);
                        string name = ReadName(offset, nameLen);
                        if (info.Name == name)
                        {
                            offset += nameLen;
                            if (offset < len && text[offset] == '>')
                            {
                                offset++;
                                PopCurrent(name);
                                return ReadName(firstIndex, lastIndex - firstIndex);
                            }
                        }
                    }
                }
                ThrowError("读取不到节点“{0}”对应的结束节点。", info.Name);
            }
            return null;
        }

        #endregion
    }

    public delegate void ParseNodeCallback(TagParser parser, IScriptBuilder builder, TagNodeInfo info);

    public delegate void CheckChildCallback(TagParser parser, IScriptBuilder builder, TagNodeInfo parent, TagNodeInfo child);

    public enum TagNodeType
    {
        Element, EndElement, WholeElement, Text
    }

    public class TagNodeInfo
    {
        private TagNodeType nodeType;
        private string name;
        private Dictionary<string, string> attrs;
        private int index, length;
        private TagNodeInfo parent, previous;
        private CheckChildCallback checkChild;
        private int state;

        internal TagNodeInfo(TagNodeType type, string name)
        {
            this.nodeType = type;
            this.name = name;
        }

        public TagNodeType NodeType
        {
            get { return nodeType; }
            internal set { nodeType = value; }
        }

        public string Name { get { return name; } }

        public Dictionary<string, string> Attrs
        {
            get
            {
                if (attrs == null) attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                return attrs;
            }
        }

        public string GetAttrValue(string name)
        {
            string result;
            if (attrs != null && attrs.TryGetValue(name, out result)) return result;
            return null;
        }

        public int Index
        {
            get { return index; }
            internal set { index = value; }
        }

        public int Length
        {
            get { return length; }
            internal set { length = value; }
        }

        public TagNodeInfo Parent
        {
            get { return parent; }
            internal set { parent = value; }
        }

        public TagNodeInfo Previous
        {
            get { return previous; }
            internal set { previous = value; }
        }

        public CheckChildCallback CheckChildHandler
        {
            get { return checkChild; }
            set { checkChild = value; }
        }

        public int Status
        {
            get { return state; }
            set { state = value; }
        }
    }

    public interface IScriptBuilder
    {
        void AppendBodyScript(string script);

        void CheckComponent(string path);

        string ArgusScript { get; set; }

        string BodyScript { get; }
    }

    public class StringScriptBuilder : IScriptBuilder
    {
        private StringBuilder buffer;
        private string argusScript;
        private List<string> componentList;

        public StringScriptBuilder()
        {
            this.buffer = new StringBuilder(128);
        }

        public void AppendBodyScript(string script)
        {
            this.buffer.Append(script);
        }

        public string BodyScript { get{ return this.buffer.ToString(); } }

        public string ArgusScript { get { return argusScript; } set { argusScript = value; } }

        public override string ToString()
        {
            return BodyScript;
        }

        public void CheckComponent(string path)
        {
            if (componentList == null) componentList = new List<string>();
            componentList.Add(path);
        }

        public string[] Components { get { return componentList == null ? null : componentList.ToArray(); } }
    }

    [AddMember("execute", ScriptComponentObject.ExecuteFunctionScript)]
    public class ScriptComponentObject : IScriptNativeProxy
    {
        #region 常量
        private const string BodyScriptTemplate = @"
function(opt) {
	var {0} = this.output.bind(this);
	var {1} = this.write.bind(this);
	var func = function() {
        {2}	
	};
	var obj = this.checkArgus(opt);
	func.call(obj);
}
";
        private const string ExecuteFunctionScript = @"
function(opt) {
    if (!this.mainFunc) {
        this.mainFunc = eval(this.script);
    }
    this.mainFunc(opt);
}
";
        #endregion

        private IScriptComponent component;
        private string path, script;
        private IScriptObject argus;
        private object nativeObjects;

        #region 构造函数

        [ObjectConstructor]
        public ScriptComponentObject(string path)
        {
            this.path = path;
        }
        
        #endregion

        #region 私有方法

        private void CheckExecutor(ScriptContext context)
        {
            if (component == null && path != null && context != null)
            {
                IScriptExecutor executor = (IScriptExecutor)context.GetCacheValue(TagParser.KEY_Executor);
                if (executor != null)
                {
                    component = executor.GetComponent(context, path);
                    if (component == null)
                        throw new Exception("找不到组件：" + path);
                    if (nativeObjects != null)
                    {
                        object native = nativeObjects;
                        nativeObjects = null;
                        ScriptNativeObject obj = native as ScriptNativeObject;
                        if (obj != null)
                            CheckComponent(context, obj, component);
                        else
                        {
                            List<ScriptNativeObject> list = (List<ScriptNativeObject>)native;
                            foreach (ScriptNativeObject item in list)
                                CheckComponent(context, item, component);
                        }
                    }
                }
            }
        }

        private void CheckComponent(ScriptContext context, ScriptNativeObject instance, IScriptComponent component)
        {
            IScriptNativeProxy proxy = component as IScriptNativeProxy;
            if (proxy != null) proxy.AfterCreated(context, instance);
        }

        #endregion

        #region 脚本方法

        [ObjectMember("script")]
        public string Script
        {
            get
            {
                if (script == null)
                {
                    CheckExecutor(ScriptContext.Current);
                    if (component != null)
                        script = ScriptGlobal.FormatString(BodyScriptTemplate, TagParser.DefaultOutputFuncName, TagParser.DefaultWriteFuncName, component.BodyScript);
                }
                return script;
            }
        }

        [ObjectMember("checkArgus")]
        public ScriptObject CheckArgus(ScriptObject options, ScriptContext context)
        {
            ScriptObject result = new ScriptObject();
            if (argus == null)
            {
                CheckExecutor(context);
                if (component != null)
                    argus = context.Eval(component.ArgusScript);
                if (argus == null) argus = ScriptUndefined.Instance;
            }
            ScriptObject source = argus as ScriptObject;
            if (!ScriptGlobal.IsNull(source))
            {
                foreach (KeyValuePair<string, IScriptObject> kv in source)
                {
                    ScriptObject v = kv.Value as ScriptObject;
                    IScriptObject v2 = v.GetValue(context, "value");
                    result.SetValue(context, kv.Key, v2 != null ? v2 : ScriptUndefined.Instance);
                }
            }
            if (!ScriptGlobal.IsNull(options))
            {
                foreach (KeyValuePair<string, IScriptObject> kv in options)
                {
                    if (result.GetValue(context, kv.Key) != null)
                        result.SetValue(context, kv.Key, kv.Value);
                }
            }
            CheckExecutor(context);
            component.CheckArgus(context, result);
            return result;
        }

        [ObjectMember("output")]
        public void Output(int index, int length, ScriptContext context)
        {
            this.component.Output(context, index, length);
        }

        [ObjectMember("write")]
        public void Write(IScriptObject value, ScriptContext context)
        {
            this.component.Write(context, value);
        }

        #endregion

        #region IScriptNativeProxy

        void IScriptNativeProxy.AfterCreated(ScriptContext context, ScriptNativeObject obj)
        {
            obj.AddScriptMember(context, "execute", ExecuteFunctionScript);
            if (component != null)
                CheckComponent(context, obj, component);
            else if (nativeObjects == null)
                nativeObjects = obj;
            else
            {
                List<ScriptNativeObject> list;
                ScriptNativeObject last = nativeObjects as ScriptNativeObject;
                if (last != null)
                {
                    list = new List<ScriptNativeObject>();
                    list.Add(last);
                }
                else
                    list = (List<ScriptNativeObject>)nativeObjects;
                list.Add(obj);
            }
        }

        #endregion
    }

    delegate void OutputFunctionCallback(ScriptContext context, int index, int length);

    delegate void WriteFunctionCallback(ScriptContext context, IScriptObject value);

    public interface IScriptOutput
    {
        void Output(ScriptContext context, int index, int length);

        void Write(ScriptContext context, IScriptObject value);
    }

    public interface IScriptExecutor : IScriptOutput
    {
        IScriptComponent GetComponent(ScriptContext context, string path);
    }

    public interface IScriptComponent : IScriptOutput
    {
        string BodyScript { get; }

        string ArgusScript { get; }

        void CheckArgus(ScriptContext context, ScriptObject thisObject);
    }
}
