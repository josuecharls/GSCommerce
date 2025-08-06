using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad.Ventas
{
    public class BeanResumenDiario
    {
        public BeanResumenDiario() { }


        public String VERSION_UBL { get; set; }
        public String CUSTOMATIZACION { get; set; }
        public String FECHA_REFERENCIA { get; set; }
        public String FECHA_EMISION { get; set; }
        public String SERIE { get; set; }
        public String NUMERO { get; set; }
        public String NUMERO_RESUMEN { get; set; }
        public String RESUMEN_GUARDAR { get; set; }


        public String RUC_EMPRESA { get; set; }
        public String TIPO_DOC_EMPRE { get; set; }
        public String NOMBRE_EMPRESA { get; set; }
        public String RAZON_EMPRESA { get; set; }

        public List<Detalles> DetallesResumen { get; set; }

        public class Detalles
        {
            public String ID_CORRELATIVO { get; set; }
            public String TIPO_DOCUMENTO { get; set; }
            public String DOCU_SERIAL_ID { get; set; }
            public String DOCU_SERIE_NUM { get; set; }
            public String STAR_DOCUMENTO { get; set; }
            //public String END_DOCUMENTO { get; set; }


            public String MONEDA_COD { get; set; }

            public String STATUS { get; set; }
            public String MONEDA_LETRA { get; set; }
            public String TOTAL_VENTA { get; set; }

            public String INSTRUCTION_ID { get; set; }
            public String INSTRUCTION_ID2 { get; set; }
            public String INSTRUCTION_ID3 { get; set; }
            public String INSTRUCTION_ID4 { get; set; }
            public String INSTRUCTION_ID5 { get; set; }

            public String TOTAL_GRAVADA { get; set; }
            public String TOTAL_EXONERADA { get; set; }
            public String TOTAL_INAFECTA { get; set; }
            public String TOTAL_EXPORTACION { get; set; }
            public String TOTAL_GRATUITA { get; set; }

            public String CHANGE_INDICATION { get; set; }
            public String AMOUNT { get; set; }

            public String COD_UBIGEO { get; set; }


            public String IGV_GRAV { get; set; }
            public String ISC_GRAV { get; set; }
            public String OTH_GRAV { get; set; }

            public String IGV_EXO { get; set; }
            public String ISC_EXO { get; set; }
            public String OTH_EXO { get; set; }

            public String IGV_INAFECTA { get; set; }
            public String ISC { get; set; }
            public String OTH { get; set; }

            public String IGV_GRAT { get; set; }
            public String ISC_GRAT { get; set; }
            public String OTH_GRAT { get; set; }

            public String IGV_EXPOR { get; set; }
            public String ISC_EXPOR { get; set; }
            public String OTH_EXPOR { get; set; }

            //AGREGADO

            public String RUC_CLIENTE { get; set; }
            public String TIPO_DOC_CLIENTE { get; set; }
            public String NOMBRE_CLIENTE { get; set; }
            public String UBIGEO_CLIENTE { get; set; }
            public String DIRECCION_CLIENTE { get; set; }
            public String URBANI_CLIENTE { get; set; }
            public String REGION_CLIENTE { get; set; }
            public String PROVINCIA_CLIENTE { get; set; }
            public String DISTRITO_CLIENTE { get; set; }
            public String PAIS_CLIENTE { get; set; }
            public String NOMBRE_COMERCIAL_CLIENTE { get; set; }
            public String TELEFONO_CLIENTE { get; set; }


            //AGREGADO
            public String COD_TIPO_NOTA { get; set; }
            public String NRO_COMPROBANTE { get; set; }
        }
    }
}
