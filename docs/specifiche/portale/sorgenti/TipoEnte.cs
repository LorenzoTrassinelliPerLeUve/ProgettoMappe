
namespace _4GrapesAPI.Entity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public partial class TipoEnte
    {
        public TipoEnte()
        {
            this.Ente = new HashSet<Ente>();
        }

        [Key] public int idTipoEnte { get; set; }
        public string Descrizione { get; set; }
    
        public virtual ICollection<Ente> Ente { get; set; }
    }
}
