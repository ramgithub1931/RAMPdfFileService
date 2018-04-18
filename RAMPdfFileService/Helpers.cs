/*
    Copyright (C) 2018 RAM Mutual Insurance Company

    This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by
    the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
    FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License along with this program. If not, see http://www.gnu.org/licenses/.  
*/
using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RAMPdfFileService
{
    public static class Helpers
    {
        public static StreamReader GetStreamReader(string path, int numSeconds = 1)
        {
            int tries = 0;
            while (!File.Exists(path))
            {
                tries++;

                if (tries >= (numSeconds * 10))
                {
                    throw new FileNotFoundException(String.Format("Could not find file {0}", path));
                }

                Thread.Sleep(100);
            }

            return new StreamReader(path);
        }

        public static PdfReader GetPdfReader(string path, int numSeconds = 1)
        {
            int tries = 0;
            while (!File.Exists(path))
            {
                tries++;

                if (tries >= (numSeconds * 10))
                {
                    throw new FileNotFoundException(String.Format("Could not find file {0}", path));
                }

                Thread.Sleep(100);
            }

            return new PdfReader(path);
        }
    }
}
