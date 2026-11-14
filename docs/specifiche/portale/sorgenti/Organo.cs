
namespace _4GrapesAPI.Entity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public partial class Organo
    {
        public Organo()
        {
            this.DocumentoIA = new HashSet<DocumentoIA>();
            this.Rilievo = new HashSet<Rilievo>();
            this.FaseDiSviluppo = new HashSet<FaseDiSviluppo>();
        }

        [Key] public int idOrgano { get; set; }
        [Column("Organo")] public string Organo1 { get; set; }
        public string Organo_EN { get; set; }
        public string Organo_RO { get; set; }
        public string Organo_ES { get; set; }
        public string Organo_FR { get; set; }
        public string Organo_RU { get; set; }
        public string Organo_DE { get; set; }
        public string Organo_PT { get; set; }
        public string Organo_SL { get; set; }
    
        public virtual ICollection<DocumentoIA> DocumentoIA { get; set; }
        public virtual ICollection<Rilievo> Rilievo { get; set; }
        public virtual ICollection<FaseDiSviluppo> FaseDiSviluppo { get; set; }
    }
}
