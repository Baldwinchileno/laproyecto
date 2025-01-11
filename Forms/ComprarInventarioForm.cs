using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using AdminSERMAC.Models;
using AdminSERMAC.Services.Database;
using Microsoft.Extensions.Logging;
using AdminSERMAC.Services;

namespace AdminSERMAC.Forms
{
    public class CompraInventarioForm : Form
    {
        private readonly ILogger<SQLiteService> _logger;
        private readonly SQLiteService _sqliteService;
        private readonly CompraRegistroDatabaseService _compraRegistroService;

        // Controles del formulario
        private DataGridView dgvCompras;
        private TextBox txtProveedor;
        private TextBox txtProducto;
        private NumericUpDown numCantidad;
        private NumericUpDown numPrecioUnitario;
        private TextBox txtObservaciones;
        private Button btnAgregar;
        private Button btnEditar;
        private Button btnEliminar;
        private Button btnProcesar;
        private Label lblTotal;

        private CompraRegistro registroSeleccionado;

        public CompraInventarioForm(SQLiteService sqliteService, ILogger<SQLiteService> logger)
        {
            _sqliteService = sqliteService ?? throw new ArgumentNullException(nameof(sqliteService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _compraRegistroService = new CompraRegistroDatabaseService(logger);

            InitializeComponents();
            CargarRegistros();
        }

        private void InitializeComponents()
        {
            this.Text = "Cuaderno de Compras - SERMAC";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Panel de entrada de datos
            var panelEntrada = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                Padding = new Padding(10)
            };

            // Etiquetas y campos de texto
            var lblProveedor = new Label { Text = "Proveedor:", Location = new Point(10, 15) };
            txtProveedor = new TextBox { Location = new Point(100, 12), Width = 200 };

            var lblProducto = new Label { Text = "Producto:", Location = new Point(10, 45) };
            txtProducto = new TextBox { Location = new Point(100, 42), Width = 200 };

            var lblCantidad = new Label { Text = "Cantidad:", Location = new Point(320, 15) };
            numCantidad = new NumericUpDown
            {
                Location = new Point(400, 12),
                Width = 100,
                DecimalPlaces = 2,
                Minimum = 0.01M,
                Maximum = 10000M,
                Value = 1M
            };

            var lblPrecio = new Label { Text = "Precio Unit.:", Location = new Point(320, 45) };
            numPrecioUnitario = new NumericUpDown
            {
                Location = new Point(400, 42),
                Width = 100,
                DecimalPlaces = 2,
                Minimum = 0.01M,
                Maximum = 1000000M,
                Value = 0.01M
            };

            var lblObservaciones = new Label { Text = "Observaciones:", Location = new Point(10, 75) };
            txtObservaciones = new TextBox
            {
                Location = new Point(100, 72),
                Width = 400,
                Multiline = true,
                Height = 40
            };

            // Botones
            btnAgregar = new Button
            {
                Text = "Agregar",
                Location = new Point(520, 12),
                Width = 100,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnEditar = new Button
            {
                Text = "Editar",
                Location = new Point(520, 42),
                Width = 100,
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };

            btnEliminar = new Button
            {
                Text = "Eliminar",
                Location = new Point(520, 72),
                Width = 100,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };

            btnProcesar = new Button
            {
                Text = "Procesar al Inventario",
                Location = new Point(630, 42),
                Width = 150,
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };

            lblTotal = new Label
            {
                Text = "Total: $0.00",
                Location = new Point(630, 72),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.75F, FontStyle.Bold)
            };

            // DataGridView
            dgvCompras = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Agregar columnas al DataGridView
            dgvCompras.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Visible = false },
                new DataGridViewTextBoxColumn { Name = "FechaCompra", HeaderText = "Fecha" },
                new DataGridViewTextBoxColumn { Name = "Proveedor", HeaderText = "Proveedor" },
                new DataGridViewTextBoxColumn { Name = "Producto", HeaderText = "Producto" },
                new DataGridViewTextBoxColumn { Name = "Cantidad", HeaderText = "Cantidad" },
                new DataGridViewTextBoxColumn { Name = "PrecioUnitario", HeaderText = "Precio Unit." },
                new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "Total" },
                new DataGridViewTextBoxColumn { Name = "Observaciones", HeaderText = "Observaciones" },
                new DataGridViewCheckBoxColumn { Name = "EstaProcesado", HeaderText = "Procesado" }
            });

            // Eventos
            btnAgregar.Click += BtnAgregar_Click;
            btnEditar.Click += BtnEditar_Click;
            btnEliminar.Click += BtnEliminar_Click;
            btnProcesar.Click += BtnProcesar_Click;
            dgvCompras.SelectionChanged += DgvCompras_SelectionChanged;
            numCantidad.ValueChanged += CalcularTotal;
            numPrecioUnitario.ValueChanged += CalcularTotal;

            // Agregar controles al panel
            panelEntrada.Controls.AddRange(new Control[]
            {
                lblProveedor, txtProveedor,
                lblProducto, txtProducto,
                lblCantidad, numCantidad,
                lblPrecio, numPrecioUnitario,
                lblObservaciones, txtObservaciones,
                btnAgregar, btnEditar, btnEliminar,
                btnProcesar, lblTotal
            });

            // Agregar controles al formulario
            this.Controls.Add(dgvCompras);
            this.Controls.Add(panelEntrada);
        }

        private async void CargarRegistros()
        {
            try
            {
                dgvCompras.Rows.Clear();
                var registros = await _compraRegistroService.GetAllCompraRegistrosAsync();

                foreach (var registro in registros)
                {
                    dgvCompras.Rows.Add(
                        registro.Id,
                        registro.FechaCompra.ToString("dd/MM/yyyy HH:mm"),
                        registro.Proveedor,
                        registro.Producto,
                        registro.Cantidad,
                        registro.PrecioUnitario.ToString("C2"),
                        registro.Total.ToString("C2"),
                        registro.Observaciones,
                        registro.EstaProcesado
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar registros de compra");
                MessageBox.Show("Error al cargar los registros de compra.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CalcularTotal(object sender, EventArgs e)
        {
            var total = numCantidad.Value * numPrecioUnitario.Value;
            lblTotal.Text = $"Total: {total:C2}";
        }

        private void LimpiarCampos()
        {
            txtProveedor.Clear();
            txtProducto.Clear();
            numCantidad.Value = 1M;
            numPrecioUnitario.Value = 0.01M;
            txtObservaciones.Clear();
            registroSeleccionado = null;
            btnEditar.Enabled = false;
            btnEliminar.Enabled = false;
        }

        private async void BtnAgregar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProveedor.Text) || string.IsNullOrWhiteSpace(txtProducto.Text))
            {
                MessageBox.Show("Por favor complete los campos obligatorios.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var nuevoRegistro = new CompraRegistro
                {
                    FechaCompra = DateTime.Now,
                    Proveedor = txtProveedor.Text,
                    Producto = txtProducto.Text,
                    Cantidad = numCantidad.Value,
                    PrecioUnitario = numPrecioUnitario.Value,
                    Total = numCantidad.Value * numPrecioUnitario.Value,
                    Observaciones = txtObservaciones.Text,
                    EstaProcesado = false
                };

                await _compraRegistroService.CreateCompraRegistroAsync(nuevoRegistro);
                CargarRegistros();
                LimpiarCampos();
                MessageBox.Show("Registro agregado exitosamente.",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar registro de compra");
                MessageBox.Show("Error al agregar el registro.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvCompras_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvCompras.CurrentRow != null)
            {
                var id = Convert.ToInt32(dgvCompras.CurrentRow.Cells["Id"].Value);
                CargarRegistroParaEdicion(id);
                btnEditar.Enabled = true;
                btnEliminar.Enabled = true;
            }
        }

        private async void CargarRegistroParaEdicion(int id)
        {
            try
            {
                registroSeleccionado = await _compraRegistroService.GetCompraRegistroByIdAsync(id);
                if (registroSeleccionado != null)
                {
                    txtProveedor.Text = registroSeleccionado.Proveedor;
                    txtProducto.Text = registroSeleccionado.Producto;
                    numCantidad.Value = registroSeleccionado.Cantidad;
                    numPrecioUnitario.Value = registroSeleccionado.PrecioUnitario;
                    txtObservaciones.Text = registroSeleccionado.Observaciones;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar registro para edición");
                MessageBox.Show("Error al cargar el registro para edición.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnEditar_Click(object sender, EventArgs e)
        {
            if (registroSeleccionado == null) return;

            try
            {
                registroSeleccionado.Proveedor = txtProveedor.Text;
                registroSeleccionado.Producto = txtProducto.Text;
                registroSeleccionado.Cantidad = numCantidad.Value;
                registroSeleccionado.PrecioUnitario = numPrecioUnitario.Value;
                registroSeleccionado.Total = numCantidad.Value * numPrecioUnitario.Value;
                registroSeleccionado.Observaciones = txtObservaciones.Text;

                await _compraRegistroService.UpdateCompraRegistroAsync(registroSeleccionado);
                CargarRegistros();
                LimpiarCampos();
                MessageBox.Show("Registro actualizado exitosamente.",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar registro");
                MessageBox.Show("Error al actualizar el registro.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (registroSeleccionado == null) return;

            if (MessageBox.Show("¿Está seguro de eliminar este registro?",
                "Confirmación", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    await _compraRegistroService.DeleteCompraRegistroAsync(registroSeleccionado.Id);
                    CargarRegistros();
                    LimpiarCampos();
                    MessageBox.Show("Registro eliminado exitosamente.",
                        "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al eliminar registro");
                    MessageBox.Show("Error al eliminar el registro.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void BtnProcesar_Click(object sender, EventArgs e)
        {
            if (dgvCompras.CurrentRow == null) return;

            var id = Convert.ToInt32(dgvCompras.CurrentRow.Cells["Id"].Value);
            try
            {
                var registro = await _compraRegistroService.GetCompraRegistroByIdAsync(id);
                if (registro.EstaProcesado)
                {
                    MessageBox.Show("Este registro ya ha sido procesado.",
                        "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Aquí deberíamos actualizar el inventario
                try
                {
                    // Actualizar el inventario usando el servicio existente
                    var inventarioService = new InventarioDatabaseService(_logger, _sqliteService.connectionString);
                    await inventarioService.AgregarProductoAsync(
                        registro.Producto,
                        registro.Cantidad,
                        registro.PrecioUnitario,
                        registro.Proveedor);

                    // Marcar el registro como procesado
                    await _compraRegistroService.MarcarComoProcesadoAsync(id);

                    CargarRegistros();
                    MessageBox.Show("Registro procesado y agregado al inventario exitosamente.",
                        "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar el registro en el inventario");
                    MessageBox.Show("Error al procesar el registro en el inventario: " + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar registro");
                MessageBox.Show("Error al procesar el registro: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}