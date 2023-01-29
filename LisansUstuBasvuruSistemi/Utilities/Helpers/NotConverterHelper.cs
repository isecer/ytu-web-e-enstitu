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
        public static CevrilenNotModel ToNotCevir(this double deger, int Sistem)
        {
            var mdl = new CevrilenNotModel();
            if (Sistem == NotSistemi.Not1LikSistem)
            {  // && CSistem == 100
                mdl.Not1Lik = 1;
                mdl.Not4Luk = 4;
                mdl.Not5Lik = 5;
                mdl.Not100Luk = (30d + (-35d / 2d + (0.5825d + (0.1925d + 0.195833d * (-2d + deger)) * (-3d + deger)) * (-1d + deger)) * (-5d + deger)).ToString("n2").ToDouble().Value;
            }
            else if (Sistem == NotSistemi.Not4LükSistem)
            {
                //&& CSistem == 100
                mdl.Not1Lik = 1;
                mdl.Not4Luk = 4;
                mdl.Not5Lik = 5;
                mdl.Not100Luk = (100d + (70d / 3d + (0.00166667d + 0.00166667d * (-2d + deger)) * (-1d + deger)) * (-4d + deger)).ToString("n2").ToDouble().Value;
            }
            else if (Sistem == NotSistemi.Not5LikSistem)
            {
                mdl.Not1Lik = 1;
                mdl.Not4Luk = 4;
                mdl.Not5Lik = 5;
                mdl.Not100Luk = (100d + (18.6667d + (-0.000952381d + (0.0021645d + 0.00155844d * (-4d + deger)) * (-3d + deger)) * (-1.25d + deger)) * (-5d + deger)).ToString("n2").ToDouble().Value;
            }
            else if (Sistem == NotSistemi.Not100LükSistem)
            {
                mdl.Not1Lik = 1;
                mdl.Not4Luk = 4;
                mdl.Not5Lik = 5;
                mdl.Not100Luk = deger.ToString("n2").ToDouble().Value;
            }
            else if (Sistem == NotSistemi.Not20LikSistem)
            {
                mdl.Not1Lik = 1;
                mdl.Not4Luk = 4;
                mdl.Not5Lik = 5;
                mdl.Not100Luk = ToNotCevir((deger * (0.2)), NotSistemi.Not4LükSistem).Not100Luk.ToString("n2").ToDouble().Value;
            }
            return mdl;

        }
        public static double toGenelBasariNotu(this double MezuniyetNotu100LukSistem, bool MulakatSurecineGirecek, BasvuruSurecOgrenimTipleri BasurecOT, bool IsAlesYerineDosyaNotuIstensin, double? AlesNotu, double? GirisSinavNotu = null)
        {

            var formul = "";

            string retVal = "";
            string reGexF = "";
            string AlesKey = IsAlesYerineDosyaNotuIstensin ? "Dosya" : "Ales";
            if (BasurecOT.OgrenimTipKod == OgrenimTipi.TezsizYuksekLisans)
            {
                if (AlesNotu.HasValue)
                {
                    // MezuniyetNotu100LukSistem + AlesNotu
                    formul = IsAlesYerineDosyaNotuIstensin ? BasurecOT.GBNFormuluD : BasurecOT.GBNFormulu;
                    reGexF = formul.Replace("Agno", MezuniyetNotu100LukSistem.ToString()).Replace(AlesKey, AlesNotu.ToString());
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
                else
                {
                    //sadece MezuniyetNotu100LukSistem  
                    reGexF = MezuniyetNotu100LukSistem.ToString();
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
            }
            else
            {


                if (AlesNotu.HasValue && GirisSinavNotu.HasValue)
                {
                    // MezuniyetNotu100LukSistem + GirisSinavNotu + AGNO 
                    formul = IsAlesYerineDosyaNotuIstensin ? BasurecOT.GBNFormuluD : BasurecOT.GBNFormulu;
                    reGexF = formul.Replace("Agno", MezuniyetNotu100LukSistem.ToString()).Replace(AlesKey, AlesNotu.ToString()).Replace("Mülakat", GirisSinavNotu.Value.ToString());
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
                else if (GirisSinavNotu.HasValue)
                {

                    // MezuniyetNotu100LukSistem + GirisSinavNotu 
                    formul = IsAlesYerineDosyaNotuIstensin ? BasurecOT.GBNFormuluDDosyasiz : BasurecOT.GBNFormuluAlessiz;
                    reGexF = formul.Replace("Agno", MezuniyetNotu100LukSistem.ToString()).Replace("Mülakat", GirisSinavNotu.Value.ToString());
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
                else if (AlesNotu.HasValue)
                {
                    // MezuniyetNotu100LukSistem + GirisSinavNotu 
                    formul = IsAlesYerineDosyaNotuIstensin ? BasurecOT.GBNFormuluDMulakatsiz : BasurecOT.GBNFormuluMulakatsiz;
                    reGexF = formul.Replace("Agno", MezuniyetNotu100LukSistem.ToString()).Replace(AlesKey, AlesNotu.Value.ToString());
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
                else
                {
                    reGexF = MezuniyetNotu100LukSistem.ToString();
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
            }
            return retVal.ToDouble().Value;

        }

    }
}