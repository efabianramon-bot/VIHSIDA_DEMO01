using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conavihsida.Core.Data
{
    public class ResultadoLogin
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; }
        public Conavihsida.Core.Models.Usuario Usuario { get; set; }
    }
}
