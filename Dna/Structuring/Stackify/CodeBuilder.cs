using System.Text;

namespace Dna.Structuring.Stackify
{
    /// <summary>
    /// Helper class for emitting and building C code.
    /// </summary>
    public class CodeBuilder
    {
        /// <summary>
        /// The amount of bit vector variables allocated.
        /// </summary>
        private int bitsetCount = 0;

        /// <summary>
        /// The current indentation level.
        /// </summary>
        private string indent = String.Empty;

        /// <summary>
        /// The buffer for generated code.
        /// </summary>
        private StringBuilder builder = new StringBuilder();

        /// <summary>
        /// Gets or sets whether logging code should be generated.
        /// </summary>
        public bool ShouldSupressLogGeneration { get; set; } = false;

        public void AddIndent(int count = 1)
        {
            while (count > 0)
            {
                count--;
                indent += "    ";
            }
        }

        public void RemoveIndent(int count = 1)
        {
            while (count > 0)
            {
                count--;
                indent = indent.Substring(0, indent.Length - 4);
            }
        }

        public void Append(string text)
        {
            var lines = text.Split(Environment.NewLine);
            foreach (var line in lines)
            {
                if (line.Length != 0) // TODO: Allow blank lines
                {
                    builder.Append(indent + line + Environment.NewLine);
                }
            }
        }

        /// <summary>
        /// Temporary hack to fix a spacing issue after refactoring. TODO: Fix and refactor out
        /// </summary>
        /// <param name="text"></param>
        public void AppendWithoutNewLine(string text)
        {
            builder.Append(text);
        }

        public void Append(string text, params object[] args)
        {
            Append(String.Format(text, args));
        }

        public void AppendLine(string text)
        {
            Append(text + Environment.NewLine);
        }

        public void AppendLine(string text, params object[] args)
        {
            AppendLine(String.Format(text, args));
        }

        public void AppendFormat(string text, params object[] args)
        {
            Append(String.Format(text, args));
        }

        /// <summary>
        /// Appends code to allocate a new bitset and set it's value;
        /// </summary>
        /// <returns></returns>
        public string AppendAndInitializeLocalBitset(int size, string valueText, params object[] args)
        {
            string varName = String.Format("bitset{0}", bitsetCount);
            AppendLine("std::bitset<{0}> {1} = {2};", size, varName, String.Format(valueText, args));
            bitsetCount++;
            return varName;
        }

        /// <summary>
        /// Appends code to allocate a new bitset, and does not set it's value.
        /// </summary>
        /// <returns></returns>
        public string AppendUninitializedLocalBitset(int size)
        {
            string varName = String.Format("bitset{0}", bitsetCount);
            AppendLine("std::bitset<{0}> {1};", size, varName);
            bitsetCount++;
            return varName;
        }

        /// <summary>
        /// Appends code to allocate a new local variable without being initialized.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public string AppendUnitializedLocalVariable(string type, string name)
        {
            AppendLine("{0} {1};", type, name);
            return name;
        }

        /// <summary>
        /// Appends code to allocate a new local variable and initialize it with the provided value.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string AppendAndInitializeLocalVariable(string type, string name, string value)
        {
            AppendLine("{0} {1} = {2};", type, name, value);
            return name;
        }

        /// <summary>
        /// Append initial forloop boilerplate, which excludes the closing bracket
        /// </summary>
        /// <param name="init"></param>
        /// <param name="condition"></param>
        /// <param name="increment"></param>
        public void StartForLoop(string init, string condition, string increment)
        {
            AppendLine("for({0}; {1}; {2})", init, condition, increment);
            Append("{");
            AddIndent();
        }

        /// <summary>
        /// Append boilerplate code to terminate a for loop
        /// </summary>
        public void EndForLoop()
        {
            RemoveIndent();
            AppendLine("}");
            //AppendLine("");
        }

        /// <summary>
        /// Append initial forloop boilerplate, which excludes the closing bracket
        /// </summary>
        /// <param name="condition"></param>
        public void StartWhileLoop(string condition)
        {
            AppendLine("while({0})", condition);
            Append("{");
            AppendLine("");
            AddIndent();
        }

        /// <summary>
        /// Append initial if statement boilerplate, which excludes the closing bracket
        /// </summary>
        /// <param name="condition"></param>
        public void StartIfStatement(string condition)
        {
            AppendLine("if({0})", condition);
            Append("{");
            AppendLine("");
            AddIndent();
        }

        /// <summary>
        /// Append initial else if boilerplate, which excludes the closing bracket
        /// </summary>
        /// <param name="condition"></param>
        public void StartElseIfStatement(string condition)
        {
            AppendLine("else if({0})", condition);
            Append("{");
            AppendLine("");
            AddIndent();
        }

        /// <summary>
        /// Append initial else statement boilerplate, which excludes the closing bracket
        /// </summary>
        /// <param name="condition"></param>
        public void StartElseStatement(string condition)
        {
            AppendLine("else", condition);
            Append("{");
            AppendLine("");
            AddIndent();
        }

        public void AppendFunctionCall(string functionName, string functionParameters)
        {
            AppendLine("{0}({1});", functionName, functionParameters);
        }

        public string AppendFunctionCallAndStoreResult(string storageVariable, string functionName, string functionParameters)
        {
            AppendLine("auto {0} = {1}({2});", storageVariable, functionName, functionParameters);
            return storageVariable;
        }

        /// <summary>
        /// Append the start of a function.
        /// </summary>
        /// <param name="returnType"></param>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <returns>The generated prototype of the function</returns>
        public string StartFunction(string returnType, string name, string parameters)
        {
            string prototype = String.Format("{0} {1}({2})", returnType, name, parameters);
            AppendLine(prototype);
            Append("{");
            AppendLine("");
            AddIndent();
            return prototype;
        }

        /// <summary>
        /// Append the start of a function that returns a bitset.
        /// </summary>
        /// <param name="bitsetSize"></param>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <returns>The generated prototype of the function</returns>
        public string StartBitsetFunction(int bitsetSize, string name, string parameters)
        {
            return StartFunction(String.Format("std::bitset<{0}>", bitsetSize), name, parameters);
        }

        /// <summary>
        /// Append boilerplate code to terminate a function
        /// </summary>
        public void EndFunction()
        {
            RemoveIndent();
            AppendLine("}");
            AppendLine("");
        }

        /// <summary>
        /// Append boilerplate code to terminate a a function, while loop, if statement, or anything that ends in brackets.
        /// </summary>
        public void EndClause()
        {
            RemoveIndent();
            AppendLine("}");
            AppendLine("");
        }

        public void AppendReturnStatement(string value)
        {
            AppendLine("return {0};", value);
        }

        public void AppendPush(string stackName, string value)
        {
            AppendLine("{0}.push({1});", stackName, value);
        }

        public void AppendPush(int bitsetSize, string value)
        {
            AppendLine("stack_bitset_{0}length.push({1});", bitsetSize, value);
        }

        public string AppendPop(string stackName, string storageVariable)
        {
            AppendLine("auto {0} = {1}.pop();", storageVariable, stackName);
            return storageVariable;
        }

        public string AppendPop(int bitsetSize, string storageVariable)
        {
            AppendLine("auto {0} = stack_bitset_{1}length.pop();", storageVariable, bitsetSize);
            return storageVariable;
        }

        /// <summary>
        /// Logs text if log suppression is disabled.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="args"></param>
        public void AppendLog(string text, params object[] args)
        {
            if (!ShouldSupressLogGeneration)
            {
                AppendLine(text, args);
            }
        }

        public override string ToString()
        {
            return builder.ToString();
        }

        public void Clear()
        {
            bitsetCount = 0;
            indent = String.Empty;
            builder = new StringBuilder();
            ShouldSupressLogGeneration = false;
        }
    }
}
