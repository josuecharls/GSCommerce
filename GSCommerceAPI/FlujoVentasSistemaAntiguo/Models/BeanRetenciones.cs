using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad.Ventas
{
    public class BeanRetenciones
    {
        public BeanRetenciones() { }

        public String VERSION_UBL { get; set; }
        public String CUSTOMATIZACION_ID { get; set; }
        public String TIPO_DOCUMENTO { get; set; }//C
        public String SERIE { get; set; }
        public String NUMERO { get; set; }
        public String NUMERO_RETENCION { get; set; }
        public String FECHA_EMISION { get; set; }

        //EMPRESA
        public String RUC_EMPRESA { get; set; }
        public String TIPO_DOC_EMPR { get; set; }
        public String NOMBRE_EMPR { get; set; }
        public String UBIGEO_EMPR { get; set; }
        public String DIRECCION_EMPR { get; set; }
        public String URB_EMPR { get; set; }
        public String PROV_EMPRESA { get; set; }
        public String DEP_EMPRESA { get; set; }
        public String DIST_EMPRESA { get; set; }
        public String CODIGO_PAIS { get; set; }
        public String RAZON_SOCIAL_EMPR { get; set; }

        //CLIENTE
        public String RUC_CLIENTE { get; set; }//M
        public String TIPO_DOC_CLIE { get; set; }//M
        public String NOMBRE_CLIE { get; set; }//C
        public String DIRECCION_CLIE { get; set; }//C
        public String UBIGEO_CLIE { get; set; }
        public String URB_CLIE { get; set; }//C
        public String PROV_CLIE { get; set; }//C
        public String DEP_CLIE { get; set; }//C
        public String DIST_CLIE { get; set; }//C
        public String CODIGO_PAIS_CLIE { get; set; }//C        
        public String RAZON_SOCIAL_CLIE { get; set; } //M

        //CABECERA
        public String CODIGO_RETENCION { get; set; }//M
        public String TAZA_RETENCION { get; set; }//M
        public String NOTA { get; set; }
        public String MONEDA { get; set; }//M
        public String IMPORTE_CAB_RETENIDO { get; set; }//M
        public String IMPORTE_CAB_TOTAL { get; set; }//C
        

        //DETALLE
     

        //DATOS DE DOCUMENTO DE REFERENCIA

        public String TIPO_DOC_REF { get; set; }//M
        public String SERIE_REF { get; set; }//M
        public String NUMERO_REF { get; set; }//M
        public String SERIE_NUMERO_REF { get; set; }//M
        public String FECHA_DOC_REF { get; set; }//M
        public String MONEDA_DOC_REF { get; set; }//M
        public String IMPOR_TOTAL_DOC_REF { get; set; } //M

        public String ID { get; set; }//M    
        public String IMPORTE_TOTAL_RET_REF { get; set; } //M
        public String FECHA_RETENCION { get; set; } //M

        public String IMPORTE_RETENIDO { get; set; }//M
        public String FECHA_PAGO_RETENCION { get; set; } //M
        public String IMPORTE_SUBTOTAL_RET_REF { get; set; } //M
        public String IMPORTE_SOLES { get; set; } //M

        public String MON_ORIGEN { get; set; }//M
        public String MON_DESTINO { get; set; }//M
        public String CALCULO_IGV { get; set; }//M
        public String FECHA_TIPO_CAMBIO { get; set; }//M
      
        
    

    }
}
