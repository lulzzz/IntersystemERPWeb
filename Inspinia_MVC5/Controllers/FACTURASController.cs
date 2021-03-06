﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Inspinia_MVC5.Models;

namespace Inspinia_MVC5.Controllers
{
    public class FACTURASController : Controller
    {
        private intersystemerpEntities db = new intersystemerpEntities();

        // GET: /FACTURAS/
        public ActionResult Index()
        {
            DateTime fecha = DateTime.Now;
            var factura = db.FACTURA.OrderByDescending(f => f.FACTURA_ID).Include(f => f.CLIENTE).Include(f => f.SERIE_DOCUMENTO).Include(f => f.USUARIO);
            var total_mes = db.FACTURA.Where(t => t.FECHA_EMISION.Value.Month == fecha.Month && t.FECHA_EMISION.Value.Year == fecha.Year).Sum(t => t.TOTAL);
            var total_anio = db.FACTURA.Where(t => t.FECHA_EMISION.Value.Year == fecha.Year).Sum(t => t.TOTAL).Value;
            var facturas = db.SERIE_DOCUMENTO.Select(f => f.HASTA).FirstOrDefault() - db.FACTURA.OrderByDescending(f => f.NRO_FACTURA).Select(f => f.NRO_FACTURA).FirstOrDefault();

            ViewBag.Total_Anio = total_anio;
            ViewBag.Facturas = facturas;
            ViewBag.Total_Mensual = total_mes;
            return View(factura.ToList());
        }

        // GET: /FACTURAS/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FACTURA fACTURA = db.FACTURA.Find(id);
            if (fACTURA == null)
            {
                return HttpNotFound();
            }
            return View(fACTURA);
        }

        // GET: /FACTURAS/Create
        public ActionResult Create()
        {
            FACTURA Factura = new FACTURA();
            //Clientes
            var clienteList = new List<SelectListItem>();
            clienteList.Add(new SelectListItem() { Value = "0", Text = "-Elija Cliente-", Selected = Factura.CLIENTE_ID == 0 });
            clienteList.AddRange(db.CLIENTE.Select(r => new SelectListItem()
            {
                Value = r.CLIENTE_ID + "",
                Text = r.NOMBRE_CLTE,
                Selected = Factura.CLIENTE_ID == r.CLIENTE_ID
            }));

            //Serie
            var seriesList = new List<SelectListItem>();
            seriesList.Add(new SelectListItem() { Value = "0", Text = "-Elija Serie-", Selected = Factura.SERIE_DOC_ID == 0 });
            seriesList.AddRange(db.SERIE_DOCUMENTO.Where(r => r.SERIE_ACTIVO == true && r.TIPO_DOC_ID == 1).Select(r => new SelectListItem()
            {
                Value = r.SERIE_DOC_ID + "",
                Text = r.SERIE,
                Selected = Factura.SERIE_DOC_ID == r.SERIE_DOC_ID
            }));

            //Tipo Producto
            var productoList = new List<SelectListItem>();
            productoList.Add(new SelectListItem() { Value = "0", Text = "-Elija Producto-", Selected = true });
            productoList.AddRange(db.PRODUCTO.Where(p => p.ACTIVO == true).Select(p => new SelectListItem()
            {
                Value = p.CODIGO_PRODUCTO + "",
                Text = p.NOMBRE,
            }));

            ViewBag.CLIENTE_ID = clienteList;
            ViewBag.SERIE_DOC_ID = seriesList;
            ViewBag.FECHA_EMISION = DateTime.Now.Date.ToShortDateString();
            ViewBag.PRODUCTO = productoList;
            ViewBag.USUARIO_ID = new SelectList(db.USUARIO, "USUARIO_ID", "NOMBRE_COMPLETO");
            return View();
        }

        // POST: /FACTURAS/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "FACTURA_ID,USUARIO_ID,CLIENTE_ID,SERIE_DOC_ID,NRO_FACTURA,FECHA_EMISION,SUBTOTAL,TOTAL,ANULADA,CAUSA_ANULADA,ESTADO_DOC,PAGOS,FECHA_ACTUALIZADO, FECHA_VENCIMIENTO")] FACTURA fACTURA)
        {
            if (ModelState.IsValid)
            {
                fACTURA.USUARIO_ID = 1;
                fACTURA.ANULADA = false;
                fACTURA.ESTADO_DOC = false;
                fACTURA.SUBTOTAL = fACTURA.TOTAL;
                fACTURA.PAGOS = 0;
                db.FACTURA.Add(fACTURA);
                db.SaveChanges();

                DOCS_CC docs_cc = new DOCS_CC();
                SERIE_DOCUMENTO sERIE = db.SERIE_DOCUMENTO.Find(fACTURA.SERIE_DOC_ID);
                docs_cc.TIPO_DOC_ID = sERIE.TIPO_DOC_ID;
                docs_cc.USUARIO_ID = 1;
                docs_cc.CLIENTE_ID = fACTURA.CLIENTE_ID;
                docs_cc.NRO_DOC = fACTURA.NRO_FACTURA;
                docs_cc.FECHA_EMISION = fACTURA.FECHA_EMISION;
                docs_cc.MONTO_DOC = fACTURA.TOTAL;
                docs_cc.MONTO_PARCIAL = fACTURA.TOTAL;
                docs_cc.FECHA_HORA = DateTime.Now;
                docs_cc.TIPO = "C";
                docs_cc.DESC_DOC = "Factura No. " + fACTURA.NRO_FACTURA;
                docs_cc.FECHA_VENCIMIENTO = fACTURA.FECHA_VENCIMIENTO;
                docs_cc.NRO_PAGOS = 0;
                docs_cc.BALANCE = fACTURA.TOTAL;
                docs_cc.ID_ORIGEN = db.FACTURA.Where(f => f.NRO_FACTURA == fACTURA.NRO_FACTURA).Select(f => f.FACTURA_ID).FirstOrDefault();
                db.DOCS_CC.Add(docs_cc);
                db.SaveChanges();

                return RedirectToAction("Create");
            }

            ViewBag.CLIENTE_ID = new SelectList(db.CLIENTE, "CLIENTE_ID", "NOMBRE_CLTE", fACTURA.CLIENTE_ID);
            ViewBag.SERIE_DOC_ID = new SelectList(db.SERIE_DOCUMENTO, "SERIE_DOC_ID", "SERIE", fACTURA.SERIE_DOC_ID);
            ViewBag.USUARIO_ID = new SelectList(db.USUARIO, "USUARIO_ID", "NOMBRE_COMPLETO", fACTURA.USUARIO_ID);
            return View(fACTURA);
        }

        // GET: /FACTURAS/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FACTURA fACTURA = db.FACTURA.Find(id);
            if (fACTURA == null)
            {
                return HttpNotFound();
            }
            ViewBag.CLIENTE_ID = new SelectList(db.CLIENTE, "CLIENTE_ID", "NOMBRE_CLTE", fACTURA.CLIENTE_ID);
            ViewBag.SERIE_DOC_ID = new SelectList(db.SERIE_DOCUMENTO, "SERIE_DOC_ID", "SERIE", fACTURA.SERIE_DOC_ID);
            ViewBag.USUARIO_ID = new SelectList(db.USUARIO, "USUARIO_ID", "NOMBRE_COMPLETO", fACTURA.USUARIO_ID);
            return View(fACTURA);
        }

        // POST: /FACTURAS/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FACTURA_ID,USUARIO_ID,CLIENTE_ID,SERIE_DOC_ID,NRO_FACTURA,FECHA_EMISION,SUBTOTAL,TOTAL,ANULADA,CAUSA_ANULADA,ESTADO_DOC,PAGOS,FECHA_ACTUALIZADO, FECHA_VENCIMIENTO")] FACTURA fACTURA)
        {
            if (ModelState.IsValid)
            {
                db.Entry(fACTURA).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CLIENTE_ID = new SelectList(db.CLIENTE, "CLIENTE_ID", "NOMBRE_CLTE", fACTURA.CLIENTE_ID);
            ViewBag.SERIE_DOC_ID = new SelectList(db.SERIE_DOCUMENTO, "SERIE_DOC_ID", "SERIE", fACTURA.SERIE_DOC_ID);
            ViewBag.USUARIO_ID = new SelectList(db.USUARIO, "USUARIO_ID", "NOMBRE_COMPLETO", fACTURA.USUARIO_ID);
            return View(fACTURA);
        }

        // GET: /FACTURAS/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FACTURA fACTURA = db.FACTURA.Find(id);
            if (fACTURA == null)
            {
                return HttpNotFound();
            }
            return View(fACTURA);
        }

        // POST: /FACTURAS/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            FACTURA fACTURA = db.FACTURA.Find(id);
            db.FACTURA.Remove(fACTURA);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        [HttpPost]
        public int? GetNumeroFactura(int? id)
        {
            int? factnumero = null;

            if (id.HasValue && id.Value > 0)
            {
                SERIE_DOCUMENTO sERIE = db.SERIE_DOCUMENTO.Find(id);
                if (sERIE.SERIE_ACTIVO.HasValue && sERIE.SERIE_ACTIVO == true)
                {
                    var numerofactura = db.FACTURA.Where(f => f.SERIE_DOC_ID == id.Value).OrderByDescending(f => f.NRO_FACTURA).Select(f => f.NRO_FACTURA).FirstOrDefault();
                    if (numerofactura.HasValue)
                    {
                        factnumero = numerofactura.Value + 1;
                    }
                    else
                    {
                        factnumero = Convert.ToInt32(sERIE.DESDE);
                    }
                    return factnumero;
                }
                else
                {
                    return -1;
                }
            }
            return factnumero;
        }

        [HttpPost]
        public (string descripcion, decimal? precio) GetDetalleProducto(int? id)
        {
            
            string descripcion = null;
            decimal? precio = null;

            if (id.HasValue && id.Value > 0)
            {
                
                PRODUCTO prod = db.PRODUCTO.Find(id);
                descripcion = prod.DESCRIPCION;
                precio = prod.PRECIO1;
            }
            
            return(descripcion, precio);
        }

        public ActionResult ReportesFacturas()
        {
            var clienteList = new List<SelectListItem>();
            clienteList.Add(new SelectListItem() {Value = "0", Text = "-Elija Cliente-"});
            clienteList.AddRange(db.CLIENTE.Select(r => new SelectListItem()
            {
                Value = r.CLIENTE_ID + "",
                Text = r.NOMBRE_CLTE
            }));
            ViewBag.cliente = clienteList;
            return View();
        }

        [HttpPost]
        public PartialViewResult FacturasPorFecha(FormCollection collection)
        {
            DateTime? fechainicio = String.IsNullOrEmpty(collection["fechaDesde"]) ? (DateTime?)null : DateTime.ParseExact(collection["FechaDesde"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime? fechafin = String.IsNullOrEmpty(collection["fechaHasta"]) ? (DateTime?)null : DateTime.ParseExact(collection["fechaHasta"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            ViewBag.fechaDesde = fechainicio.Value.ToShortDateString();
            ViewBag.fechaHasta = fechafin.Value.ToShortDateString();

            var consulta = (from f in db.FACTURA
                join c in db.CLIENTE on f.CLIENTE_ID equals c.CLIENTE_ID
                orderby f.NRO_FACTURA
                select new ReporteFacturacion
                {
                   CODIGO_CLTE = c.CODIGO_CLTE,
                   NOMBRE_CLTE = c.NOMBRE_CLTE,
                   NRO_FACTURA = f.NRO_FACTURA,
                   FECHA_EMISION = f.FECHA_EMISION,
                   TOTAL = f.TOTAL,
                   ANULADA = f.ANULADA
                });

            if (fechainicio != null && fechafin != null)
            {
                consulta = consulta.Where(r => r.FECHA_EMISION >= fechainicio && r.FECHA_EMISION <= fechafin);
            }

            return PartialView("FacturasPorFecha", consulta.ToList());
        }

        [HttpPost]
        public PartialViewResult FacturasAnuladasPorFecha(FormCollection collection)
        {
            DateTime? fechainicio = String.IsNullOrEmpty(collection["fechaDesde"]) ? (DateTime?)null : DateTime.ParseExact(collection["FechaDesde"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime? fechafin = String.IsNullOrEmpty(collection["fechaHasta"]) ? (DateTime?)null : DateTime.ParseExact(collection["fechaHasta"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);

            ViewBag.fechaDesde = fechainicio.Value.ToShortDateString();
            ViewBag.fechaHasta = fechafin.Value.ToShortDateString();

            var consulta = (from f in db.FACTURA
                join c in db.CLIENTE on f.CLIENTE_ID equals c.CLIENTE_ID
                            where f.ANULADA == true
                select new ReporteFacturacion
                {
                    CODIGO_CLTE = c.CODIGO_CLTE,
                    NOMBRE_CLTE = c.NOMBRE_CLTE,
                    NRO_FACTURA = f.NRO_FACTURA,
                    FECHA_EMISION = f.FECHA_EMISION,
                    TOTAL = f.TOTAL,
                    ANULADA = f.ANULADA
                });

            if (fechainicio != null && fechafin != null)
            {
                consulta = consulta.Where(r => r.FECHA_EMISION >= fechainicio && r.FECHA_EMISION <= fechafin);
            }

            return PartialView("FacturasAnuladasPorFecha", consulta.ToList());
        }

        [HttpPost]
        public PartialViewResult FacturasPorCliente(FormCollection collection)
        {
            DateTime? fechainicio = String.IsNullOrEmpty(collection["fechaDesde"]) ? (DateTime?)null : DateTime.ParseExact(collection["FechaDesde"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime? fechafin = String.IsNullOrEmpty(collection["fechaHasta"]) ? (DateTime?)null : DateTime.ParseExact(collection["fechaHasta"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
            int cliente;

            if (!collection.AllKeys.Contains("cliente") || string.IsNullOrEmpty(collection["cliente"]))
                cliente = 0;
            else
                cliente = Convert.ToInt32(collection["cliente"]);

            ViewBag.fechaDesde = fechainicio.Value.ToShortDateString();
            ViewBag.fechaHasta = fechafin.Value.ToShortDateString();
            ViewBag.cliente = db.CLIENTE.Where(c => c.CLIENTE_ID == cliente).Select(c => c.NOMBRE_CLTE)
                .FirstOrDefault();

            var consulta = (from f in db.FACTURA
                join c in db.CLIENTE on f.CLIENTE_ID equals c.CLIENTE_ID
                orderby f.NRO_FACTURA
                select new ReporteFacturacion
                {
                    CLIENTE_ID = c.CLIENTE_ID,
                    CODIGO_CLTE = c.CODIGO_CLTE,
                    NOMBRE_CLTE = c.NOMBRE_CLTE,
                    NRO_FACTURA = f.NRO_FACTURA,
                    FECHA_EMISION = f.FECHA_EMISION,
                    TOTAL = f.TOTAL,
                    ANULADA = f.ANULADA
                });

            if (fechainicio != null && fechafin != null)
            {
                consulta = consulta.Where(r => r.FECHA_EMISION >= fechainicio && r.FECHA_EMISION <= fechafin);
            }

            if (cliente != 0)
            {
                consulta = consulta.Where(r => r.CLIENTE_ID == cliente);
            }

            return PartialView("FacturasPorCliente", consulta.ToList());
        }
    }
}
