using System;
using System.Diagnostics;
using System.IO;

namespace GnuPlot
{
    public static class GnuPlot
    {
        public static GnuPlotExecutor With(string dataPath) => new GnuPlotExecutor(dataPath);
    }

    public struct GnuPlotExecutor
    {
        private ProcessStartInfo _processInfo;
        private const string TEMPLATE_PATH = "templates/basic.plt";

        public GnuPlotExecutor(string dataPath)
        {
            CheckTemplateEncoding();
            _processInfo = new ProcessStartInfo("gnuplot.exe")
            {
                ArgumentList =
                {
                    "-c",
                    TEMPLATE_PATH,
                    // Argv: http://www.gnuplot.info/docs_5.4/Gnuplot_5_4.pdf
                     dataPath
                }
            };
        }

        public void Open()
        {
            Process.Start(_processInfo);
        }


        private static void CheckTemplateEncoding()
        {
            using (var stream = File.OpenRead(TEMPLATE_PATH))
            {
                var bomBuffer = new byte[3];
                stream.Read(bomBuffer, 0, 3);
                // https://en.wikipedia.org/wiki/Byte_order_mark#UTF-8
                if (bomBuffer[0] == 0xEF && bomBuffer[1] == 0xBB && bomBuffer[2] == 0xBF)
                {
                    throw new NotSupportedException(
                        "Template encoding doesn't support BOM UTF8 format. Use UTF8 without BOM or ASCII instead.");
                }
            }
        }
    }
}
