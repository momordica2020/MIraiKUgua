﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using MMDK.Util;

namespace MMDK.Mods
{
    /// <summary>
    /// 随机掷骰子
    /// </summary>
    public class ModDice : Mod
    {

        public bool Init(string[] args)
        {

            return true;
        }

        public void Exit()
        {
            
        }

        public bool HandleText(long userId, long groupId, string message, List<string> results)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            Regex reg = new Regex(@"^r(\d*)?d(\d*)?(.*)?$");
            var result = reg.Match(message);
            if (result.Success)
            {
                int dicenum = 1;
                int facenum = 100;
                string desc = "";
                try
                {
                    if (result.Groups.Count == 4)
                    {
                        try
                        {
                            dicenum = int.Parse(result.Groups[1].ToString());
                            if (dicenum > 100) dicenum = 100;
                        }
                        catch { }
                        try
                        {
                            facenum = int.Parse(result.Groups[2].ToString());
                        }
                        catch { }
                        try
                        {
                            desc = result.Groups[3].ToString();
                        }
                        catch { }
                    }
                }
                catch { }
                string resdesc = "";
                long res = getRoll(facenum, dicenum, out resdesc);
                results.Add($"{desc} {dicenum}d{facenum} = {resdesc}");
                return true;
            }
            return false;
        }














        public long getRoll(int faceNum, int DiceNum, out string resdesc)
        {
            long res = 0;
            List<long> ress = new List<long>();
            for (int i = 0; i < DiceNum; i++)
            {
                ress.Add(faceNum > 1 ? MyRandom.Next(faceNum) + 1 : 1);
            }
            res = ress.Sum();
            if (DiceNum == 1) resdesc = $"{res}";
            else resdesc = $"{string.Join("+", ress)} = {res}";
            return res;
        }
    }


}
