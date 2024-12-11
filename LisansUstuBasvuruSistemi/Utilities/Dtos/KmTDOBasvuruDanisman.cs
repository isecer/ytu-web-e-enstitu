using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using Newtonsoft.Json;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmTdoBasvuruDanisman : TDOBasvuruDanisman
    {
        public bool? IsCopy;

        public new bool? IsTezDiliTr { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public SelectList SListSinav { get; set; }
        public SelectList SListSinavNot { get; set; }
        public SelectList SListTdAnabilimDali { get; set; }
        public SelectList SListTdProgram { get; set; }
        public SelectList SListTDoDanismanTalepTip { get; internal set; }

        public int? TezBaslikMaxLength { get; set; }
        public string TezBaslikIllegalCharacter { get; set; }

        public string GenerateSpecialCharacterBlockerScript()
        {
            var tezBaslikIllegalCharacter =
                string.IsNullOrEmpty(TezBaslikIllegalCharacter) ? "" : TezBaslikIllegalCharacter;
            var encodedTChrctr =
                JsonConvert.SerializeObject(tezBaslikIllegalCharacter); // illegal karakterleri JSON formatına çevir
            var maxLength = TezBaslikMaxLength.HasValue? TezBaslikMaxLength.ToString() :"null";
            return $@"
                        <script> 
                                var tChrctr = {encodedTChrctr};  // C# tarafında oluşturulan JSON verisi
                                var mTLength = {maxLength};  // TezBaşlıkMaxLength değeri

                                var invalidTChars = tChrctr ? tChrctr.split(',') : [];

                                new SpecialCharacterBlocker('tCharacterBlock', invalidTChars, mTLength);
                             
                        </script>
                        ";
        }
    }
}