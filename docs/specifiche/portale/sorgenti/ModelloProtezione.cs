namespace _4GrapesAPI.Entity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using _4Grapes.Traduzioni;
    using _4GrapesAPI.Properties;

    public partial class ModelloProtezione
    {
        //[Key] public int Id { get; set; }
        [Key, Column(Order = 0)] public DateTime Data { get; set; }
        [Key, Column(Order = 1)] public int IdTrattamentoAvversita { get; set; }
        [Key, Column(Order = 2)] public int IdSostanzaAttiva { get; set; }
        public decimal PercEfficaciaProdotto { get; set; }
        public decimal PercDilavamento { get; set; }
        public decimal PercProtezione { get; set; }
        public decimal SommaPrecipitazioni { get; set; }
        public decimal SommaOreBagnatura { get; set; }
        public bool MeteoPrevisionale { get; set; }

        [ForeignKey(nameof(IdSostanzaAttiva))] public virtual SostanzaAttiva SostanzaAttiva { get; set; }
        [ForeignKey(nameof(IdTrattamentoAvversita))] public virtual TrattamentoAvversita TrattamentoAvversita { get; set; }


        public static string GetTestoDaValore(decimal? valore)
        {
            if (valore == null || valore <= 0)
            {
                return Tr.nessuna.ToUpper();
            }
            else if (valore < 70m)
            {
                return Tr.scarsa.ToUpper();
            }
            else if (valore <= 80m)
            {
                return Tr.media.ToUpper();
            }
            else
            {
                return Tr.buona.ToUpper();
            }
        }
    }
}
