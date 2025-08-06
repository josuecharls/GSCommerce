using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad.Ventas
{
    public class BeanNotaCredito
    {
        public BeanNotaCredito() { }

        public String ID_DOC_VENTA { get; set; }
        public String PROFILE_ID { get; set; }
        public String HORA_EMISION { get; set; }

        public String MONEDA_COD { get; set; }
        public String MONEDA_LETRA { get; set; }

        public String NRO_GUIA_REMISION { get; set; }
        public String NRO_ORDEN_COMPRA { get; set; }
        public String COD_GUIA_REMISION { get; set; }

        public String CANT_ITEM { get; set; }
        public String ORDER_REFERENCIA { get; set; }
       
        public String TOTAL_GRAVADA { get; set; }
        public String TOTAL_GRATUITA { get; set; }
        public String TOTAL_INAFECTA { get; set; }
        public String TOTAL_DESCUENTO { get; set; }
        public String TOTAL_EXONERADA { get; set; }
        public String SUB_TOTAL { get; set; }
        public String IGV_TOTAL { get; set; }
        public String ISC { get; set; }
        public String OTH { get; set; }

        public String TOTAL { get; set; }
        public String MONTO_TOTAL_LETRAS { get; set; }


        public String VERSION_UBL { get; set; }
        public String CUSTOMATIZACION { get; set; }
        public String SERIE { get; set; }
        public String NUMERO { get; set; }
        public String NRO_NOTA_CREDITO { get; set; }
        public String FECHA_EMISION { get; set; }
        public String TIPO_DOCUMENTO { get; set; }

        public String SERIE_NRO_ANULAR { get; set; }//NUMERO Y SERIE DE DOCUMENTO ASOCIADO
        public String COD_NOTA_CREDITO { get; set; }//TIPO DE NOTA DE CREDITO
        public String DESC_NOTA_CREDITO { get; set; }//DESCRIPCION DE NOTA DE CREDITO REFERENTE AL CODIGO


        public String SERIE_NRO_ASOCIADO { get; set; }
        public String TIPO_DOC_ASOCIADO { get; set; }

        public String RUC_EMPRESA { get; set; }
        public String TIPO_DOC_EMPRE { get; set; }
        public String NOMBRE_EMPRESA { get; set; }
        public String UBIGEO_EMPRE { get; set; }
        public String DIRECCION_EMPRE { get; set; }
        public String URBANIZACION_EMPRE { get; set; }
        public String REGION_EMPRE { get; set; }
        public String PROVINCIA_EMPRE { get; set; }
        public String DISTRITO_EMPRE { get; set; }
        public String PAIS_EMPRE { get; set; }
        public String RAZON_COMERCIAL_EMPRE { get; set; }


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


       
        public String ALLOWANCETOTALAMOUNT { get; set; }
        public String CHANGETOTALAMOUNT { get; set; }
  

        //DETALLE
        public String DET_ID_CORRELATIVO { get; set; }
        public String DET_COD_UNIDAD { get; set; }
        public String DET_UNIDAD_LETRA { get; set; }
        public String DET_CANTIDAD { get; set; }

        public String DET_PRECIO { get; set; }
        public String DET_PRECIO_ALTERNATIVO { get; set; }
        public String DET_COD_PRECIO_UNI { get; set; }
        public String DET_COD_AFECT_IGV { get; set; }

        public String DET_SUBTOTAL_ITEM { get; set; }
        public String DET_IGV_ITEM { get; set; }
        public String DET_PRECIO_IGV { get; set; }
        public String DET_TOTAL_ITEM { get; set; }


        public String DET_GLOSA { get; set; }
        public String DET_COD_ITEM { get; set; }

        public String DET_ID_ITEM { get; set; }
  
        public String DET_CANTIDAD_ITEM { get; set; }

        public String DET_PRECIO_ITEM { get; set; }   

        public String DET_COD_AFECTACION { get; set; }
        public String DET_COD_CLASIFICACION { get; set; }
        public String DET_PORCENTAJE { get; set; }     
        public String DET_PRECIO_SIN_IGV { get; set; }


        public String DET_TOTAL_GRAVADA { get; set; }
        public String DET_TOTAL_GRATUITA { get; set; }
        public String DET_TOTAL_INAFECTA { get; set; }
        public String DET_TOTAL_EXONERADA { get; set; }
        public String DET_SUB_TOTAL { get; set; }
        public String DET_DESCUENTO { get; set; }
        public String DET_IGV { get; set; }
        public String DET_ISC { get; set; }
        public String DET_OTH { get; set; }
        public String DET_TOTAL { get; set; }

    }
}
