## GSCommerce

GSCommerce es un Sistema de Gestión Comercial desarrollado con ASP.NET Core Web API y Blazor WebAssembly, utilizando .NET 8 como framework principal. Este proyecto tiene como objetivo administrar la información de una tienda comercial, incluyendo gestión de usuarios, clientes, productos, almacenes, ventas, y más.

## Tecnologías Utilizadas

**Backend:** ASP.NET Core Web API  
**Frontend:** Blazor WebAssembly  
**Base de Datos:** SQL Server con Entity Framework Core  
**Autenticación:** JSON Web Token (JWT)  
**Almacenamiento Local:** Blazored.LocalStorage  
**Control de Versiones:** Git y GitHub  

## Estructura del Proyecto

**GSCommerce/**

**│── GSCommerceAPI/**         # API en ASP.NET Core

**│── GSCommerce.Client/**     # Blazor WebAssembly (Frontend)

**│── GSCommerce.sln**         # Archivo de solución

**│── README.md**              # Documentación del proyecto

## Configuración y Configuración Inicial

### Clona el Repositorio
```sh
git clone https://github.com/josuecharls/GSCommerce.git
cd GSCommerce
```

### Configurar la Base de Datos

El proyecto utiliza SQL Server, y la conexión está definida en el archivo appsettings.json de GSCommerceAPI:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=SYSCHARLES;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;"
}
```

- Si se cambia los valores de conexión, se va a actualizar SyscharlesContext en el proyecto API.

### Ejecutar Migraciones (Entity Framework Core)

Si deseas regenerar la base de datos en tu entorno:
```sh
cd GSCommerceAPI
dotnet ef database update
```

Nota: La base de datos ya fue generada con dotnet ef dbcontext scaffold.

## Funcionalidades Implementadas

### Backend (GSCommerceAPI)

- ASP.NET Core Web API con patrones RESTful
- Autenticación y Autorización con JWT
- Manejo de Usuarios y Roles
- Conexión a SQL Server con Entity Framework Core
- Controladores REST: Usuarios, Personal, Clientes, Almacen, Articulos
- Soporte para imágenes en Base64
- Swagger habilitado para documentación y pruebas

### Frontend (GSCommerce.Client)

- Blazor WebAssembly como SPA (Standalone)
- Consumo de API con HttpClient
- Autenticación con JWT y Blazored.LocalStorage
- Navegación protegida por roles (Admin, Cajero, etc.)
- Menú dinámico según el cargo del usuario
- Login elegante con diseño responsivo y animaciones
- Paginación y búsqueda dinámica

## Autenticación y Seguridad

### Proceso de Autenticación

1. Se usa JWT para validar los usuarios.
2. Al iniciar sesión, el backend genera un token y lo envía al frontend.
3. Blazor WebAssembly almacena este token en LocalStorage.
4. El sistema usa Claims para manejar roles y permisos.

## Roles y Permisos Implementados

- **ADMINISTRADOR:** Acceso total al sistema.
- **CAJERO:** Acceso restringido a módulos específicos.

## Cómo Ejecutar el Proyecto

### Iniciar el Backend (GSCommerceAPI)
```
cd GSCommerceAPI
dotnet run
```

### Iniciar el Frontend (GSCommerce.Client)
```
cd GSCommerce.Client
dotnet run
```
- La API se ejecutará en https://localhost:7246 - El cliente Blazor se ejecutará en https://localhost:7018

## Git y Versionado

### Ramas Principales

- main → Rama estable con código listo para producción.
- charles → Rama de desarrollo donde se realizan las modificaciones.

### Flujo de Trabajo

- Trabajar en la rama charles
```sh
git checkout charles
```
Subir cambios a GitHub
```sh
git add .
git commit -m "Descripción del cambio"
git push origin charles
```
Fusionar con main cuando esté listo
```sh
git checkout main
git merge charles
git push origin main
```
## Licencia

Este proyecto es de uso privado y está en desarrollo.
**Creado por Cipiriano Corporation.** 
