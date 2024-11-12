﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;



namespace Kugua
{
    /// <summary>
    /// 随机掷骰子
    /// </summary>
    public class ModDice : Mod
    {

        public override bool Init(string[] args)
        {
            ModCommands[new Regex(@"^r(\d*)?d(\d*)?(.*)?$")] = handleDice;

            return true;
        }

        private string handleDice(MessageContext context, string[] param)
        {
            int dicenum = 1;
            int facenum = 100;
            string desc = "";
            try
            {
                if (param.Length == 4)
                {
                    if (int.TryParse(param[1], out dicenum))
                    {
                        dicenum = Math.Min(dicenum, 100);
                    }
                    if (int.TryParse(param[2],out facenum))
                    {

                    }
                    desc = param[3].Trim();
                }
            }
            catch { }
            string resdesc = "";
            long res = getRoll(facenum, dicenum, out resdesc);
            return ($"{desc} {dicenum}d{facenum} = {resdesc}");
        }

        public bool HandleText(long userId, long groupId, string message, List<string> results)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            Regex reg = new Regex(@"^r(\d*)?d(\d*)?(.*)?$");
            var result = reg.Match(message);
            if (result.Success)
            {
                
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