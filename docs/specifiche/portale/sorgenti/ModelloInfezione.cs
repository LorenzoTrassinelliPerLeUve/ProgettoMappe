
namespace _4GrapesAPI.Entity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using _4Grapes.Traduzioni;
    using _4GrapesAPI.Properties;

    public partial class ModelloInfezione
    {
        //[Key] public int Id { get; set; }
        [Key, Column(Order = 0)] public DateTime Data { get; set; }
        [Key, Column(Order = 1)] public int IdVigneto { get; set; }
        [Key, Column(Order = 2)] public int IdAvversitaDaTrattare { get; set; }
        public Nullable<int> IdBBCH { get; set; }
        public decimal PercInfezione { get; set; }
        public bool MeteoPrevisionale { get; set; }
        public Nullable<decimal> ProbabilitaInfezione { get; set; }

        [ForeignKey(nameof(IdAvversitaDaTrattare))] public virtual AvversitaDaTrattare AvversitaDaTrattare { get; set; }
        [ForeignKey(nameof(IdBBCH))] public virtual BBCH BBCH { get; set; }
        [ForeignKey(nameof(IdVigneto))] public virtual Vigneto Vigneto { get; set; }

        public static string GetTestoDaValore(decimal? valore)
        {
            if (valore == null)
            {
                return "-";
            }
            else if (valore < 2.5m)
            {
                return Tr.molto_basso.ToUpper();
            }
            else if (valore < 10m)
            {
                return Tr.basso.ToUpper();
            }
            else if (valore < 50m)
            {
                return Tr.medio.ToUpper();
            }
            else if (valore < 90m)
            {
                return Tr.alto.ToUpper();
            }
            else
            {
                return Tr.molto_alto.ToUpper();
            }
        }
    }
}
