using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Text.RegularExpressions;
using System.Reflection;

namespace WCFServiceCalc
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Service1 : IServiceCalc
    {
        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }

        public string MefCalculateIt( string strInput )
        {
            string strRet = "";

            AssemblyCatalog ac = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            CompositionContainer cc = new CompositionContainer(ac);

            var cal = cc.GetExportedValue<MefCalculator>();

            try
            {
                var res = cal.DoMefCalculator(strInput);
                strRet = res.ToString();

                System.Diagnostics.Trace.TraceInformation("Input {0}, Result: " + strRet, strInput);
            }
            catch( Exception ex)
            {
                strRet = ex.Message;
                System.Diagnostics.Trace.TraceError("Input {0}, error: " + strRet, strInput);
            }

            return strRet;
        }
    }


    [Export]
    public class MefCalculator
    {
        [ImportMany]
        private IEnumerable<Lazy<Func<int, int, int>, IOperationMetadata>> operationParts;

        [Import("CalculatorParse")]
        private Func<string, ParserResultModel> operationParser;

        public int DoMefCalculator(string s)
        {
            ParserResultModel res = operationParser(s);

            var op = operationParts.First(n => n.Metadata.operationName == res.op);
            int finalRes = op.Value(res.x, res.y);

            return finalRes;
        }
    }

    public interface IOperationMetadata
    {
        string operationName { get; }
    }

    [MetadataAttribute]
    public class OperationMetadataAttribute : Attribute, IOperationMetadata
    {
        public string operationName { get; private set; }

        public OperationMetadataAttribute(string s)
        {
            operationName = s;
        }
    }

    public class MefCalculatorOperator
    {
        [Export]
        [OperationMetadata("+")]
        public int Add(int x, int y)
        {
            return x + y;
        }

        [Export]
        [OperationMetadata("-")]
        public int sub(int x, int y)
        {
            return x - y;
        }
    }

    public class MefCalculatorParser
    {
        [Export("CalculatorParse")]
        public ParserResultModel ParserInput(string s)
        {
            Regex rg = new Regex(@"(\d+)(.)(\d+)");
            var matached = rg.Match(s);

            return new ParserResultModel
            {
                x = Int32.Parse(matached.Groups[1].ToString()),
                y = Int32.Parse(matached.Groups[3].ToString()),
                op = matached.Groups[2].ToString()
            };
        }
    }

    public class ParserResultModel
    {
        public string op { get; set; }
        public int x { get; set; }
        public int y { get; set; }
    }

}
