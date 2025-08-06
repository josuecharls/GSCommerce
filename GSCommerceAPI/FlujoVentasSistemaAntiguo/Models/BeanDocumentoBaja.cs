using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad.Ventas
{
    public class BeanDocumentoBaja
    {
        public BeanDocumentoBaja() { }

        public String DB_VERSION_UBL { get; set; }
        public String DB_COD_MONEDA { get; set; }
        public String DB_MONEDA_LETRA { get; set; }
        public String DB_CUSTOMATIZACION { get; set; }
        public String DB_FECHA_EMISION { get; set; }
        public String DB_HORA { get; set; }
        public String DB_SERIE { get; set; }
        public String DB_NUMERO { get; set; }
        public String DB_SERIE_NUMERO { get; set; }
        public String DB_REFERENCIA { get; set; }
        public String DB_FECHA { get; set; }

        public String DB_RUC { get; set; }
        public String DB_TIPO_DOC_IDENTIDAD { get; set; }
        public String DB_RAZON_SOCIAL { get; set; }
        public String DB_NOMBRE_COMERCIAL { get; set; }


        public String DB_NUM { get; set; }
        public String DB_TIPO_DOCUMENTO { get; set; }
        public String DB_SERIE_ASOCIADO { get; set; }
        public String DB_NUMERO_ASOCIADO { get; set; }
        public String DB_GLOSA { get; set; }

    }
}
