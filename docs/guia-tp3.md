## GUÍA DE FORMACIÓN PRÁCTICA

## Trabajo Práctico N° 3: Gestión de Configuración y Pipeline CI/CD

## Diagrama de las partes involucradas para este TP

## 1. ¿Qué es Docker y como funciona?

Es una plataforma que permite empaquetar una aplicación junto con todas sus dependencias, librerías y configuración del sistema operativo en una unidad estandarizada y liviana llamada Contenedor.

Conceptos claves:

- \- Dockerfile: archivo con instrucciones paso a paso para construir la imagen.

- \- Imagen (Image): plantilla de solo lectura que contiene el código, el runtime y las librerías necesarias.

- \- Contenedor (Container): instancia en ejecución de una imagen. Si la imagen es una clase de POO, el contenedor es el objeto instanciado.

- \- Registro (Registry): repositorio en la nube o local donde se guardan las imágenes (Dcoker Hub, Azure Container Registry, etc.).

- \- Multi Stage build: técnica en Docker que permite usar una imagen pesada con herramientas para compilar el proyecto y luego copiar solo los binarios compilados a una imagen final mucho más liviana (Runtime)

## 2. Explicación línea por línea de Dockerfile propuesto


El Dockerfile propuesto utiliza una estrategia Multi Stage build dividida en dos etapas: build (construcción) y final (ejecución).

```
1: # syntax=docker/dockerfile:1
2:
3: FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
4: WORKDIR /src
5: COPY . .
6: RUN dotnet publish Dsw2025Tpi.Api/Dsw2025Tpi.Api.csproj -c
Release -o /app/publish
7:
8: FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
9: WORKDIR /app
10: ENV ASPNETCORE_URLS=http://+:8080
11: ENV ASPNETCORE_ENVIRONMENT=Production
12: EXPOSE 8080
13: COPY --from=build /app/publish .
14: ENTRYPOINT ["dotnet", "Dsw2025Tpi.Api.dll"]
```

- Línea 1 (# syntax=docker/dockerfile:1):

Especifica la versión de la sintaxis del parser de Dockerfile (BuildKit) que se va a utilizar.

- Línea 3 (FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build):

Descarga la imagen oficial de Microsoft con el SDK completo de .NET 8 (contiene compiladores, CLI, NuGet, etc.). Se le asigna el alias build para referenciarla más adelante.

- Línea 4 (WORKDIR /src):

- Crea el directorio /src dentro del contenedor temporal y se posiciona en él como directorio de trabajo actual.

Línea 5 (COPY . .): Copia todo el código fuente del proyecto desde la máquina anfitriona (tu máquina o el agente de GitHub) hacia el directorio /src del contenedor.

- Línea 6 (RUN dotnet publish Dsw2025Tpi.Api/... -c Release -o /app/publish):

Ejecuta el comando de compilación y publicación de .NET en configuración optimizada (Release). El resultado final (los archivos .dll y binarios listos) se guarda en la carpeta /app/publish.

Línea 8 (FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final): Inicia una nueva imagen limpia basada únicamente en el runtime de ASP.NET Core 8. Esta imagen no tiene compiladores ni herramientas pesadas, lo que la hace mucho más pequeña, rápida y segura para producción.

- Línea 9 (WORKDIR /app):

Define /app como la carpeta de trabajo dentro del contenedor final.


- Línea 10 (ENV ASPNETCORE_URLS=http://+:8080):

Configura la variable de entorno para que el servidor web Kestrel escuche peticiones HTTP en el puerto 8080 en cualquier interfaz de red (+).

- Línea 11 (ENV ASPNETCORE_ENVIRONMENT=Production):

Establece el entorno de ejecución en modo Producción.

Línea 12 (EXPOSE 8080): Documenta y comunica que el contenedor expone el puerto 8080 para tráfico entrante.

Línea 13 (COPY --from=build /app/publish .): Clave del multi-stage: Copia únicamente los archivos compilados generados en la etapa build

(/app/publish) hacia el directorio actual (/app) de la imagen final. El código fuente y el SDK no se incluyen en la imagen final.

Línea 14 (ENTRYPOINT ["dotnet", "Dsw2025Tpi.Api.dll"]): Indica el comando que se ejecuta automáticamente cuando el contenedor arranca: inicia la API de .NET.

## 3. ¿Cómo ejecutar el contenedor localmente?

Prerequisitos:

- \- Tener instalado y abierto Docker Desktop.

- \- Abrir una terminal en la raiz del repositorio backend.

- 3.1: Construir la imagen local

En la terminal ejecutar

```
docker build -t dsw2025tpi-api:local .
```

El parámetro -t le asigna un number (etiqueta o tag) a la imagen. El . indica que el contexto del contrucción es la carpeta actual.


## 3.2: Inicializar el contenedor:

```
docker run -d -p 8080:8080 -name mi-api-backend dsw2025tpi-api:local
```

- \- -d (detached): ejecuta contenedor en segundo plano.

- \- -p 8080:8080: mapea el puerto de la máquina física al puerto 8080 del contenedor (puerto_host:puerto_contenedor)

- \- –name mi-api-backend: nombre para identificar el contenedor.

Si no obtienen ningun error, pueden ingresar a http://localhost:8080/swagger

## 4. Pipeline CI/CD con GitHub Actions

¿Qué es CI/CD y Github Actions?

- \- CI (Integración Continua): Automatiza la compilación y validación del código cada vez que alguien envía cambios al repositorio.

- \- CD (Despliegue Continuo): Automatiza la publicación de la aplicación en el servidor/nube.

- \- Github Actions: Herramienta de automatización integrada a GitHub que ejecuta flujos de trabajo (workflow) en máquinas virtuales en la nube cuando ocurre eventos de git

## 4.1: Configuración inicial del workflow:

Dentro del repositorio, se crea una carpeta .github/workflows/{nombre de archivo}.yml

```
name: Build and Deploy to Azure App Service (Publish Profile)
on:
push:
branches: [development, main]
pull_request:
branches: [development, main]
workflow_dispatch:
env:
DOCKERHUB_USERNAME: vchibilisco
IMAGE_NAME: ics-backend
jobs:
build-and-deploy:
runs-on: ubuntu-latest
steps:
- name: 1. Obtener el código
uses: actions/checkout@v4
- name: 2. Iniciar sesión en Docker Hub
```


```
uses: docker/login-action@v3
with:
```

```
username: \${{ env.DOCKERHUB_USERNAME }}
password: ${{ secrets.DOCKERHUB_TOKEN }}
- name: 3. Construir la imagen Docker (.NET 8)
run: |
docker build \
-t ${{ env.DOCKERHUB_USERNAME }}/${{ env.IMAGE_NAME
}}:${{ github.sha }} \
-t ${{ env.DOCKERHUB_USERNAME }}/${{ env.IMAGE_NAME
}}:latest \
-f Dockerfile \
.
- name: 4. Subir la imagen a Docker Hub
if: github.event_name != 'pull_request'
run: |
docker push ${{ env.DOCKERHUB_USERNAME }}/${{
env.IMAGE_NAME }}:${{ github.sha }}
docker push ${{ env.DOCKERHUB_USERNAME }}/${{
env.IMAGE_NAME }}:latest
- name: 5. Desplegar el contenedor en Azure App Service
if: github.event_name != 'pull_request'
uses: azure/webapps-deploy@v3
with:
app-name: ${{ secrets.APP_SERVICE_NAME }}
publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE
}}
images: ${{ env.DOCKERHUB_USERNAME }}/${{ env.IMAGE_NAME
}}:latest
```


## 4.2: Partes del yml

```
name: Build and Deploy to Azure App Service (Publish Profile)
on:
push:
branches: [development, main]
pull_request:
branches: [development, main]
workflow_dispatch:
env:
REGISTRY: ${{ secrets.ACR_NAME }}.azurecr.io
IMAGE_NAME: dsw2025tpi-api
```

## on: (Disparadores/Triggers):

- push: Se dispara cuando se suben commits a las ramas development o main.

- pull_request: Se dispara cuando se abre o actualiza una PR hacia development o main (sirve para validar que la imagen compila antes de mergear).

- workflow_dispatch: Permite disparar el despliegue manualmente desde la interfaz web de GitHub con un clic.

## env: (Variables de entorno del workflow):

- Define la URL del registro de contenedores de Azure (REGISTRY) usando un secreto de GitHub (secrets.ACR_NAME) y el nombre base de la imagen (IMAGE_NAME).

```
\- name: 1. Obtener el código
```

Clona el repositorio git dentro de la máquina virtual de GitHub Actions para que los pasos siguientes tengan acceso al código fuente y al Dockerfile.

```
\- name: 2. Iniciar sesión en Docker Hub
```

Se autentica contra el registro Docker Bub. Utiliza credenciales seguras almacenadas en los Github Secrets (secrets.DOCKERHUB_USERNAME y secrets.DOCKERHUB_TOKEN) para poder subir imágenes


posteriormente. DOCKERHUB_TOKEN es un nuevo Access Token creado en Docker Hub pero con permisos de lectura, escritura y borrado (ver punto 4).

```
\- name: 3. Construir la imagen Docker (.NET 8)
```

Construye la imagen ejecutando Dockerfile.

Etiquetas:

- \- Con el SHA del commit (github.sha) permite tener trazabilidad exacta de qué versión de código generó esa imagen.

- \- Con etiqueta latest representamos la versión más reciente construido.

```
\- name: 4. Subir la imagen a Docker Hub
if: github.event_name != 'pull_request'
```

Envía las imagenes hacia Docker Hub.

Si event_name != “pull_request” este step se ejecuta.

```
\- name: 5. Desplegar el contenedor en Azure App Service
if: github.event_name != 'pull_request'
```

Le indica a Azure App Service que debe actualizarse usando la nueva imagen generada

Parámetros:

- \- App-name: Nombre del App Service en Azure.

- \- Publish-profile: Perfil de publicación.

- \- Images: Versión de la imagen a ser desplegada.

Si event_name != “pull_request” este step se ejecuta.


## 4. Configuración en Docker Hub

Deben tener una cuenta de usuario en Docker Hub (https://hub.docker.com/). [URL 🔗](https://hub.docker.com/)

En su perfil, crear un repositorio. Puede ser público o privado. En esta guía se mostrará con público.

Una vez creado, debemos configurar el acceso entrando a Profile -> Account Settings -> Personal Access Token -> Generate new token


## 5. Conectar Github Actions con con Docker Hub

Debemos configurar en nuestro repositorios los secrets que serán usados por Github Actions posteriormente.

Ir al repositorio -> Settings -> Secrets and variables -> Actions, crear un New repository secret.

Se debe agregar:

- \- DOCKERHUB_USERNAME: usuario de Docker Hub.

- \- DOCKERHUB_TOKEN: Access token con permisos de lectura, escritura y borrado.

## 6. Microsoft Azure

En el buscador escribir App Services, y crear una Web App.

Importante en la configuración:

- \- Name


- \- Publish: Debe ser Container

- \- Region: Dependiendo del que elijan, puede o no estar accesible plan Free.

- \- Pricing Plan: Asegurarse que tiene seleccionado Free F1

Pasar al step de Container y se configura lo siguiente.

## Opciones:

- \- Image type: public

- \- Registry server url: index.docker.io [URL 🔗](http://index.docker.io)

- \- Image and tag: el definido en el pipeline.


- \- Port: 8080

- \- Startup command: dotnet Dsw2025Tpi.Api.dll

Hacer click en Review + create, y si no hay problemas, Create se habilitará. Hacer click y el proceso de

creación comenzará, toma unos minutos.

Cuando finalice, en App Services veremos esto:

- 7. Conectar Github Actions con Azure App Service

Debemos configurar en nuestro repositorios los secrets que serán usados por Github Actions posteriormente.

Ir al repositorio -> Settings -> Secrets and variables -> Actions.

Se debe agregar:

- \- APP_SERVICE_NAME: nombre de servicio de Azure App Service

- \- AZURE_WEBAPP_PUBLISH_PROFILE: desde Azure App Service, descargar el archivo publish profile, y su contenido asignar a esta variable en Github.


## 8. Conectar Azure App Service con SQL DataBase

8.1 Crear un servidor de base de datos

Si no tienen previamente creado un servidor, es necesario crear uno. Importante los datos para conectarse, agregar usuario y password.

Home Azure SQL | SQL databases Create SQL Database

## Create SQL Database Server

Microsoft

## Server details

Enter required settings for this server, including providing a name and location. This server will be created in the same subscription and resource group as your database.

[liestest

west us 2

Server name *

Location *

## Authentication

@ Azure Active Directory (Azure AD) is now Microsoft Entra ID. Learn

Select your preferred authentication methods for accessing this server. We recommend using only Microsoft Entra authentication Learn more using an existing Microsoft Entra user, group, or application as Microsoft Entra admin Learn more

windows.net


## 8.2 Crear Base de datos

## En Azure, buscar el servicio SQL Database, y crear una Base de datos free

Una vez finalizado la creacion, se debe copiar el CS ADO.NET (SQL authentication) agregando el

password que ingresaron para el servidor.

NOTA: En mi caso, creo una variable de entorno en App Service y copio el CS de la base de datos. Se que no es la forma correcta, estoy generando una deuda técnica, la cual debe ser corregida.
