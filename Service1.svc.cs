using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.IO;
using System.ServiceModel.Activation;

namespace TFTService
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.PerCall)]
    public class MainService : IMainService
    {
        readonly Dictionary<string, string> numberSet = new Dictionary<string, string>();
        private string prevNumberInserted = "";
        private int maxNumberLength = 0;
        private bool isThousandInserted = false;
        public string errorLog(string error)
        {
            string errorFile = @"D:\home\site\storage\tft-data\error.log";
            FileStream fs;
            if (!File.Exists(errorFile))
            {
                fs = File.Create(errorFile);
            }
            else
            {
                fs = File.Open(errorFile, FileMode.Append, FileAccess.Write);
            }
            using (StreamWriter stream = new StreamWriter(fs))
            {
                stream.WriteLine(DateTime.Now + " - " + error);
            }
            return error;
        }

        private bool LoadNumbers()
        {
            // string numberFilePath = @"D:\home\site\storage\tft-data\numbers.txt";
            string numberFilePath = @"C:\Users\Alexandru\Desktop\tfg\numbers.txt";
            if (File.Exists(numberFilePath))
            {
                using (StreamReader sr = new StreamReader(numberFilePath))
                {
                    while (sr.Peek() >= 0)
                    {
                        string[] vs = sr.ReadLine().Split(';');
                        numberSet.Add(vs[0], vs[1]);
                    }
                    sr.Close();
                }
                return true;
            }
            else
            {
                return false;
                // throw new Exception("Fichero de números no encontrado.");
            }
        }
        /**
         * 
         * <summary>Computes number.</summary>
         * <param name="input">Input number.</param>
         * 
         */
        [WebGet]
        public string ComputeNumber(string input)
        {
            /**
             * initialize service
             *
             */
            if (this.numberSet.Count == 0) this.LoadNumbers();
            string[] vs = input.Split(' ');
            string finalResult = "";
            bool isMinusInserted = false;
            for (int i = 0; i < vs.Length; i++)
            {
                string fullNumberName = vs[i];
                vs[i] = removePluralsAndFeminineTypes(vs[i]);
                if (numberSet.ContainsKey(vs[i]))
                {
                    if (isThousandInserted && numberSet[vs[i]] == "1000")
                    {
                        return "Número inválido: No se puede insertar dos veces el número 'mil' sin que haya ninguna unidad adicional insertada.";
                    } else if (numberSet[vs[i]] == "1000")
                    {
                        isThousandInserted = true;
                    }
                    if (prevNumberInserted.Length >= 6 && numberSet[vs[i]].Length >= 6 && !numberSet[vs[i]].Contains("/"))
                    {
                        return "Número inválido: No se puede introducir un número mayor que un millón seguido de otro mayor que un millón. Número erróneo: " + fullNumberName;
                    } else if (maxNumberLength != 0 && numberSet[vs[i]].Length >= 6 && numberSet[vs[i]].Length >= maxNumberLength && !numberSet[vs[i]].Contains("/"))
                    {
                        return "Número inválido: No se puede poner un número con una unidad mayor detrás de otro con una unidad menor. Número erróneo: " + fullNumberName;
                    } else if (numberSet[vs[i]].Length >= 6 && !numberSet[vs[i]].Contains("/"))
                    {
                        isThousandInserted = false;
                        maxNumberLength = numberSet[vs[i]].Length;
                    }
                    finalResult = JoinNumbersInString(finalResult, numberSet[vs[i]]);
                }
                else
                {
                    if (vs[i] == "menos")
                    {
                        if (!isMinusInserted)
                        {
                            isMinusInserted = true;
                            continue;
                        }
                        else
                        {
                            return "Error: Número inválido (no puede aparecer más de una vez la palabra \"menos\"";
                            // throw new Exception("Error: Número inválido (no puede aparecer más de una vez la palabra \"menos\"");
                        }
                    }
                    if (vs[i] == "y")
                    {
                        if (prevNumberInserted.Length == 2 && 
                            (
                                i+1 < vs.Length && numberSet.ContainsKey(vs[i+1]) && numberSet[vs[i+1]].Length != 2
                            )
                        )
                        {
                            // see if there are any suffixes beyond the next number to insert
                            if (i + 2 >= vs.Length) continue;
                            // check the rest of the array for "y" or for fractions.
                            bool charFound = false;
                            for (int j = i + 2; j < vs.Length; j++)
                            {
                                if (vs[j] == "y" || vs[j].Contains("avo"))
                                {
                                    charFound = true;
                                    break;
                                }
                            }
                            if (charFound) continue;
                        }
                    };
                    if (vs[i] == "con" || vs[i] == "coma" || vs[i] == "y")
                    {
                        string decimalResult = "";
                        prevNumberInserted = "";
                        maxNumberLength = 0;
                        isThousandInserted = false;
                        bool addedDecimal = false;
                        for (int j = i + 1; j < vs.Length; j++)
                        {
                            fullNumberName = vs[j];
                            if (vs[j].Contains("écimo") || vs[j].Contains("ésimo"))
                            {
                                return "Error: Número inválido";
                            }
                            if (!numberSet.ContainsKey(vs[j]) && !vs[j].Contains("ésima") && !vs[j].Contains("écima"))
                            {
                                vs[j] = removePluralsAndFeminineTypes(vs[j]);
                            }
                            if (numberSet.ContainsKey(vs[j]) && !vs[j].Contains("ésima") && !vs[j].Contains("écima") && !(numberSet[vs[j]].Contains("/")))
                            {
                                if (isThousandInserted && numberSet[vs[j]] == "1000")
                                {
                                    return "Número inválido: No se puede insertar dos veces el número 'mil' sin que haya ninguna unidad adicional insertada.";
                                }
                                else if (numberSet[vs[j]] == "1000")
                                {
                                    isThousandInserted = true;
                                }
                                if (prevNumberInserted.Length >= 6 && numberSet[vs[j]].Length >= 6 && !numberSet[vs[j]].Contains("/"))
                                {
                                    return "Número inválido: No se puede introducir un número mayor que un millón seguido de otro mayor que un millón. Número erróneo: " + fullNumberName;
                                }
                                else if (maxNumberLength != 0 && numberSet[vs[j]].Length >= 6 && numberSet[vs[j]].Length >= maxNumberLength && !numberSet[vs[j]].Contains("/"))
                                {
                                    return "Número inválido: No se puede poner un número con una unidad mayor detrás de otro con una unidad menor. Número erróneo: " + fullNumberName;
                                }
                                else if (numberSet[vs[j]].Length >= 6 && !numberSet[vs[j]].Contains("/"))
                                {
                                    isThousandInserted = false;
                                    maxNumberLength = numberSet[vs[j]].Length;
                                }
                                decimalResult = JoinNumbersInString(decimalResult, numberSet[vs[j]]);
                            }
                            else
                            {
                                if (vs[j] == "y") continue;
                                // This should do the shifting.
                                if (vs[j].Contains("ésima") || vs[j].Contains("écima"))
                                {
                                    int nShifts = ComputeDecimalShifts(vs[j]);
                                    if (decimalResult.Length > nShifts)
                                    {
                                        return "Error: Número inválido. Motivo: Se solicitó un número con más cifras que unidades decimales (cifras: " + decimalResult.Length + "; unidades decimales: " +
                                            nShifts + ").";
                                    }
                                    decimalResult = ShiftDecimalToRightOfNumber(decimalResult, nShifts);
                                    addedDecimal = true;
                                    break;
                                }
                                else break;
                            }
                        }
                        if (!input.Contains("ésima"))
                        {
                            addedDecimal = true;
                        }
                        if (addedDecimal)
                        {
                            finalResult += "." + decimalResult;
                            break;
                        }
                        continue;
                    }
                    if (vs[i].Contains("avo") || vs[i].Contains("ava") || vs[i].Contains("ésimo") || vs[i].Contains("ésima"))
                    {
                        isThousandInserted = false;
                        maxNumberLength = 0;
                        finalResult = finalResult.Trim();
                        string numbers = ComputeFractionNumbers(vs[i]);
                        finalResult += "/" + ComputeNumber(numbers);
                        break;
                    }
                    return "Error: Número " + vs[i] + " inválido";
                }
            }
            // Separate cardinals.
            string[] parts = finalResult.Split('.');
            string[] fractionParts = finalResult.Split('/');
            if (fractionParts.Length > 1 && parts.Length > 1) return "Error: Número inválido";
            if (fractionParts.Length > 1)
            {
                var leftFraction = SeparateNumbers(fractionParts[0].Trim(), "left").Trim();
                if (leftFraction == "") leftFraction = "1";
                return leftFraction + "/" + fractionParts[1].Trim();
            }
            finalResult = SeparateNumbers(parts[0], "left").Trim();
            if (parts.Length > 1)
            {
                finalResult += "." + SeparateNumbers(parts[1], "right").Trim();
            }
            if (finalResult[0] == '/')
            {
                finalResult.Insert(0, "1");
            }
            if (isMinusInserted) finalResult = finalResult.Insert(0, "-");
            return finalResult.Trim();
        }
        private string removePluralsAndFeminineTypes(string textNum)
        {
            if (textNum == "" || numberSet.ContainsKey(textNum)) return textNum;
            string result = textNum;
            result = result.Replace("es", String.Empty).Replace("llon", "llón");
            if (numberSet.ContainsKey(result)) return result;
            result = textNum;
            result = result.Replace("as", "os");
            if (numberSet.ContainsKey(result)) return result;
            result = result.Replace("os", "o");
            if (numberSet.ContainsKey(result)) return result;
            StringBuilder sb = new StringBuilder(textNum);
            if (sb[sb.Length - 1] == 'a') sb[sb.Length - 1] = 'o';
            result = sb.ToString();
            if (numberSet.ContainsKey(result)) return result;
            else return textNum;
        }
        /**
         * 
         * <summary>Method that extracts numbers from a fraction, ended in "-avo" or "-ava".</summary>
         * <param name="numbers">The fraction numbers.</param> 
         * <returns>A string with the numbers separated by spaces that can be used to be computed.</returns>
         * 
         */
        private string ComputeFractionNumbers(string numbers)
        {
            List<string> resultNumbers = new List<string>();
            if (numbers.Contains("tavo") || numbers.Contains("tava"))
            {
                numbers = numbers.Replace("vos", String.Empty);
                numbers = numbers.Replace("vas", String.Empty);
                numbers = numbers.Replace("vo", String.Empty);
                numbers = numbers.Replace("va", String.Empty);
            }
            else if (numbers.Contains("ésimo") || numbers.Contains("ésima"))
            {
                numbers = numbers.Replace("ésimos", String.Empty);
                numbers = numbers.Replace("ésimas", String.Empty);
                numbers = numbers.Replace("ésimo", String.Empty);
                numbers = numbers.Replace("ésima", String.Empty);
            }
            else
            {
                numbers = numbers.Replace("avos", String.Empty);
                numbers = numbers.Replace("avas", String.Empty);
                numbers = numbers.Replace("avo", String.Empty);
                numbers = numbers.Replace("ava", String.Empty);
            }
            int lastPos = numbers.Length;
            for (int i = numbers.Length - 1; i >= 0; i--)
            {
                string partNumber = numbers.Substring(i, lastPos - i);
                switch (partNumber)
                {
                    case "i":
                        lastPos = i;
                        break;
                    case "veint":
                        partNumber = "veinte";
                        break;
                    case "diec":
                        partNumber = "diez";
                        break;
                    case "cient":
                        partNumber = "ciento";
                        break;
                    case "cent":
                        partNumber = "ciento";
                        break;
                }
                if (partNumber.Contains("llon") && !partNumber.Contains("llones"))
                {
                    partNumber = partNumber.Replace("llon", "llón");
                }
                if (numberSet.ContainsKey(partNumber))
                {
                    lastPos = i;
                    resultNumbers.Add(partNumber);
                }
            }
            resultNumbers.Reverse();
            return string.Join(" ", resultNumbers);
        }
        /**
         * 
         * <summary>Method to compute the amount of shifts a decimal should go through.</summary>
         * <param name="number">Number ended in '-ésima'.</param>
         * <returns>Amount of shifts a number should go through.</returns>
         * 
         */
        private int ComputeDecimalShifts(string number)
        {
            string originalNumber = number;
            if (numberSet.ContainsKey(number))
            {
                return numberSet[number].Length - 1;
            }
            if (number.Contains("décima"))
            {
                return 1;
            }
            if (number.Contains("ésimas")) number = number.Replace("ésimas", String.Empty);
            else if (number.Contains("ésima")) number = number.Replace("ésima", String.Empty);
            else if (number.Contains("écima")) number = number.Replace("écima", String.Empty);
            else number = number.Replace("écimas", String.Empty);
            if (number.Contains("llon") && !number.Contains("llones"))
            {
                number = number.Replace("llon", "llón");
            }
            if (numberSet.ContainsKey(number))
            {
                return numberSet[number].Length - 1;
            }
            // Special cases like "a hundred thousand millionth"
            string fractionNumber = "";
            if (originalNumber.Contains("ésima"))
            {
                // last number
                fractionNumber = ComputeFractionNumbers(originalNumber);
                fractionNumber = ComputeNumber(fractionNumber).Replace(" ", String.Empty);
            }

            return fractionNumber.Length - 1;
        }
        /**
         * 
         * <summary>Method for separating strings each three characters from right to left.</summary>
         * <param name="str">The string to be separated.</param>
         * <returns>The separated string.</returns>
         * 
         */
        private string SeparateNumbers(string str, string orientation)
        {
            if (orientation == "left")
            {
                str = ReverseString(str);
            }
            for (int i = 0; i < str.Length; i += 4)
            {
                str = str.Substring(0, i) + " " + str.Substring(i);
            }
            if (orientation == "left")
            {
                return ReverseString(str);
            }
            else
            {
                return str;
            }
        }
        /**
         * <summary>Method to reverse a string easily.</summary>
         * <param name="str">String to be reversed.</param>
         * <returns>The reversed string</returns>
         * 
         */
        private string ReverseString(string str)
        {
            char[] vs = str.ToCharArray();
            Array.Reverse(vs);
            return new string(vs);
        }
        /**
         * 
         * <summary>Shifts a character to the left in the number (eg., if we wanted to shift "40" two times in "20040", the result would be "24000").</summary>
         * <param name="c">Character to be shifted.</param>
         * <param name="s">String where to shift the character.</param>
         * <param name="nShifts">Amount of positions to be shifted.</param>
         * <returns>The param s with the shifted character.</returns>
         * 
         */
        private string ShiftCharToLeftOfNumber(char c, string s, int nShifts)
        {
            string result = s;
            StringBuilder sb = new StringBuilder(result);
            for (int i = s.Length - 1; i >= 0; i--)
            {
                if (s[i] == c && s[i] != '0')
                {
                    if (i - nShifts <= 0)
                    {
                        return result += "0";
                    }
                    else
                    {
                        sb[i - nShifts] = c;
                        sb[i] = '0';
                    }
                    break;
                }
            }
            result = sb.ToString();
            return result;
        }
        /**
         * 
         * <summary>Function that shifts decimals to the right, if necessary.
         * Used if unit is smaller than the computed number (like 1 millionth).</summary>
         * <param name="convertedNumber">The number to be treated.</param>
         * <param name="nShifts">Number of times to perform shifts.</param>
         * <returns>Computed number.</returns>
         *
         */
        private string ShiftDecimalToRightOfNumber(string decimalNumber, int nShifts)
        {
            if (decimalNumber.Length >= nShifts) return decimalNumber;
            string result = decimalNumber;
            for (int i = 0; i < nShifts - decimalNumber.Length; i++)
            {
                result = "0" + result;
            }
            return result;
        }
        /**
         * 
         * <summary>Joins two number strings into one.</summary>
         * <param name="num1">Number to be joined.</param>
         * <param name="num2">Number to join.</param>
         * <returns>The joined numbers.</returns>
         * 
        **/
        private string JoinNumbersInString(string num1, string num2)
        {
            string result = "";
            if (num1 == "y" || num2 == "y") return "";
            if (num2.Contains("/")) return num1 + num2;
            if (num1 == "")
            {
                prevNumberInserted = num2;
                return num2;
            }
            if (prevNumberInserted.Length > num2.Length)
            {
                result = num1;
                StringBuilder sb = new StringBuilder(result);
                int posNum1 = result.Length - 1;
                int posNum2 = num2.Length - 1;
                while (posNum2 >= 0)
                {
                    sb[posNum1] = num2[posNum2];
                    posNum1--;
                    posNum2--;
                }
                result = sb.ToString();
            }
            else
            {
                result = num1;
                if (num1.Length > num2.Length)
                {
                    string initialString = removeStartingZeros(result.Substring(result.Length - 6));
                    for (int j = 0; j < initialString.Length; j++)
                    {
                        result = ShiftCharToLeftOfNumber(initialString[j], result, num2.Length - 1);
                    }
                }
                else
                {
                    for (int i = 1; i < num2.Length; i++)
                    {
                        if (num1.Length < num2.Length) result += "0";
                    }
                }

            }
            prevNumberInserted = num2;
            return result;
        }

        /**
         * 
         * <summary>Method used to remove all starting zeros from a number.</summary>
         * <example>"003500" -> "3500"</example>
         * <param name="num">The number to have its starting zeros removed.</param>
         * <returns>The number without the starting zeros.</returns>
         * 
         */
        private string removeStartingZeros(string num)
        {
            StringBuilder sb = new StringBuilder();
            bool numberFound = false;
            for (int i = 0; i < num.Length; i++)
            {
                if (numberFound) sb.Append(num[i]);
                else if (num[i] != '0')
                {
                    numberFound = true;
                    sb.Append(num[i]);
                }
            }
            return sb.ToString();
        }
        /**
         * 
         * <summary>Method to get a single number from the number set.</summary>
         * <param name="textNum">Number written as text.</param>
         * <returns>Number written as number.</returns>
         * 
         */
        public string GetSingleNumber(string textNum)
        {
            if (numberSet.ContainsKey(textNum))
            {
                return numberSet[textNum];
            }
            else
            {
                return "No se pudo encontrar el número " + textNum;
            }
        }
        /**
         * 
         * <summary>Method to get the list of words off a file.</summary>
         * <returns>The list containing the words.</returns>
         * 
         */
        public Dictionary<string, string> GetNumbers()
        {
            if (this.numberSet.Count == 0) this.LoadNumbers();
            return numberSet;
        }
    }
}
