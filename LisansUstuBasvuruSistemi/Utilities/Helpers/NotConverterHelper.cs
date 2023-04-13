using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class NotConverterHelper
    {
        public static CevrilenNotDto ToNotCevir(this double deger, int notSistemi)
        {
            var mdl = new CevrilenNotDto();
            if (notSistemi == NotSistemi.Not1LikSistem)
            {  // && CSistem == 100
                mdl.Not1Lik = 1;
                mdl.Not4Luk = 4;
                mdl.Not5Lik = 5;
                mdl.Not100Luk = (30d + (-35d / 2d + (0.5825d + (0.1925d + 0.195833d * (-2d + deger)) * (-3d + deger)) * (-1d + deger)) * (-5d + deger)).ToString("n2").ToDouble().Value;
            }
            else if (notSistemi == NotSistemi.Not4LükSistem)
            {
                //&& CSistem == 100
                mdl.Not1Lik = 1;
                mdl.Not4Luk = 4;
                mdl.Not5Lik = 5;
                mdl.Not100Luk = (100d + (70d / 3d + (0.00166667d + 0.00166667d * (-2d + deger)) * (-1d + deger)) * (-4d + deger)).ToString("n2").ToDouble().Value;
            }
            else if (notSistemi == NotSistemi.Not5LikSistem)
            {
                mdl.Not1Lik = 1;
                mdl.Not4Luk = 4;
                mdl.Not5Lik = 5;
                mdl.Not100Luk = (100d + (18.6667d + (-0.000952381d + (0.0021645d + 0.00155844d * (-4d + deger)) * (-3d + deger)) * (-1.25d + deger)) * (-5d + deger)).ToString("n2").ToDouble().Value;
            }
            else if (notSistemi == NotSistemi.Not100LükSistem)
            {
                mdl.Not1Lik = 1;
                mdl.Not4Luk = 4;
                mdl.Not5Lik = 5;
                mdl.Not100Luk = deger.ToString("n2").ToDouble().Value;
            }
            else if (notSistemi == NotSistemi.Not20LikSistem)
            {
                mdl.Not1Lik = 1;
                mdl.Not4Luk = 4;
                mdl.Not5Lik = 5;
                mdl.Not100Luk = ToNotCevir((deger * (0.2)), NotSistemi.Not4LükSistem).Not100Luk.ToString("n2").ToDouble().Value;
            }
            return mdl;

        }
        public static double ToGenelBasariNotu(this double mezuniyetNotu100LukSistem, bool mulakatSurecineGirecek, BasvuruSurecOgrenimTipleri basurecOt, bool isAlesYerineDosyaNotuIstensin, double? alesNotu, double? girisSinavNotu = null)
        {

            var formul = "";

            string retVal = "";
            string reGexF = "";
            string AlesKey = isAlesYerineDosyaNotuIstensin ? "Dosya" : "Ales";
            if (basurecOt.OgrenimTipKod == OgrenimTipi.TezsizYuksekLisans)
            {
                if (alesNotu.HasValue)
                {
                    // MezuniyetNotu100LukSistem + AlesNotu
                    formul = isAlesYerineDosyaNotuIstensin ? basurecOt.GBNFormuluD : basurecOt.GBNFormulu;
                    reGexF = formul.Replace("Agno", mezuniyetNotu100LukSistem.ToString()).Replace(AlesKey, alesNotu.ToString());
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
                else
                {
                    //sadece MezuniyetNotu100LukSistem  
                    reGexF = mezuniyetNotu100LukSistem.ToString();
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
            }
            else
            {


                if (alesNotu.HasValue && girisSinavNotu.HasValue)
                {
                    // MezuniyetNotu100LukSistem + GirisSinavNotu + AGNO 
                    formul = isAlesYerineDosyaNotuIstensin ? basurecOt.GBNFormuluD : basurecOt.GBNFormulu;
                    reGexF = formul.Replace("Agno", mezuniyetNotu100LukSistem.ToString()).Replace(AlesKey, alesNotu.ToString()).Replace("Mülakat", girisSinavNotu.Value.ToString());
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
                else if (girisSinavNotu.HasValue)
                {

                    // MezuniyetNotu100LukSistem + GirisSinavNotu 
                    formul = isAlesYerineDosyaNotuIstensin ? basurecOt.GBNFormuluDDosyasiz : basurecOt.GBNFormuluAlessiz;
                    reGexF = formul.Replace("Agno", mezuniyetNotu100LukSistem.ToString()).Replace("Mülakat", girisSinavNotu.Value.ToString());
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
                else if (alesNotu.HasValue)
                {
                    // MezuniyetNotu100LukSistem + GirisSinavNotu 
                    formul = isAlesYerineDosyaNotuIstensin ? basurecOt.GBNFormuluDMulakatsiz : basurecOt.GBNFormuluMulakatsiz;
                    reGexF = formul.Replace("Agno", mezuniyetNotu100LukSistem.ToString()).Replace(AlesKey, alesNotu.Value.ToString());
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
                else
                {
                    reGexF = mezuniyetNotu100LukSistem.ToString();
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
            }
            return retVal.ToDouble().Value;

        }

    }
}