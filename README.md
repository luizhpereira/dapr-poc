# Dapr-dev - Prova de conceito

# Base de conhecimento
* [Documentação Dapr](https://docs.dapr.io/getting-started)  
* [Dapr self-hosted doc](https://docs.dapr.io/operations/hosting/self-hosted/)  
* [Dapr sidecar overview](https://docs.dapr.io/concepts/dapr-services/sidecar/)
* [Dapr em Containers (Docker-Compose)](https://docs.dapr.io/operations/hosting/self-hosted/self-hosted-with-docker/)  
* [Dapr CLI comandos](https://docs.dapr.io/reference/cli/dapr-run/)
* [O básico sobre Dapr e o que ele resolve](https://learn.microsoft.com/pt-br/dotnet/architecture/dapr-for-net-developers/dapr-at-20000-feet)  
* [Imagens Docker para ASP.NET Core](https://learn.microsoft.com/pt-pt/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-6.0)
* [Linux Container com IDE Visual Studio 2022](https://learn.microsoft.com/en-us/visualstudio/containers/tutorial-multicontainer?view=vs-2022)  
* [gRPC Tests - Postman](https://blog.postman.com/postman-now-supports-grpc/)
* [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows?tabs=azure-cli)
* [Sintaxe YAML para Azure Cloud Services](https://learn.microsoft.com/en-us/azure/devops/pipelines/yaml-schema/?view=azure-pipelines)
* [Multi-Container com Docker-Compose](https://learn.microsoft.com/pt-br/azure/container-instances/tutorial-docker-compose)
* [Azure Container App Environment](https://learn.microsoft.com/en-us/azure/container-apps/overview)  
* [Azure Tipo de Conexões Ingress](https://learn.microsoft.com/en-us/azure/container-apps/ingress?tabs=bash)
* [CLI - Azure Container App](https://learn.microsoft.com/en-us/cli/azure/service-page/azure%20container%20apps?view=azure-cli-latest)
* [Azure Container App - Temp Storage Mount](https://learn.microsoft.com/en-us/azure/container-apps/storage-mounts?pivots=aca-cli)
* [Vídeo: ACAPP + Dapr - Impressões](https://youtu.be/ak2gN5IzzKw)


# Requerimentos caso use VSCode
* [Kit mínimo para VSCode](https://code.visualstudio.com/docs/languages/dotnet)  
* [Comandos .NET CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/)  
* [VSCode MVC overview](https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/adding-controller?view=aspnetcore-6.0&tabs=visual-studio-code)  

# Imagens Docker utilizadas
* [ASP.NET Core](https://hub.docker.com/_/microsoft-dotnet-aspnet)  
* [Redis:Alpine](https://hub.docker.com/_/redis)
* [Openzipkin:zipkin-slim](https://hub.docker.com/r/openzipkin/zipkin-slim)


# Dapr instalação 
  Implementação do Dapr em ambiente Windows, utilize no CLI `dapr --help` para suporte

## Download *dapr.msi*
  [Download Dapr.msi](https://github.com/dapr/cli/releases) e verifique a instalação:
  ```console 
  dapr 
  ```

## Inicializar containers Dapr
```console
dapr init
dapr --version
```
depois verifique se os containers estão a correr
```console
docker ps
```
## Configs Dapr
a localização dos ficheiros de configuração estão por default em `C:\Users\<UserName>\.dapr`

## Testes mínimos
levante uma app vazia apenas com o sidecar oferecendo uma interface API básica
```console
dapr run --app-id myapp --dapr-http-port 3500
```
Depois basta fazer uma requisição a esta API
```console
curl -X POST -H "Content-Type: application/json" -d '[{ "key": "name", "value": "Bruce Wayne"}]' http://localhost:3500/v1.0/state/statestore
```
Verifique a requisição em `http://<endereço-ip>:9411` e através de CLI com o comando `dapr list`

# Cenário de testes
  #### O cenário de testes tem como objetivo criar 3 apps, ambas protegidas por sidecars Dapr que comunicam-se. Para além disso faremos uso do gerenciador de estados e tracer Dapr.
 
 ## NUGETs
* ### Dapr.Client 1.9.0
  Para uso genérico em qualquer aplicação. Possui as diversas interfaces para todos os cenários que o Dapr atua (blocks).
* ### Dapr.AspNetCore 1.9.0
  Para uso em soluções atuais e específicas de Asp.NET. Há pré-configurações relevantes para facilitar a implementação dos cenários em que o Dapr atua (blocks) em aplicações Asp.NET Core.

## Update da prova de conceito
- 3 Apps, sendo:
  - WebFrontEnd
  - WebAPI
  - gRPCServer
- Comunicações garantidas:
  - pubsub(MQTT)
  - gRPC
  - HTTP
- Implementações
  - Dapr Sidecar 
  - Dapr Configs
  - Dapr Components
  - Atores
  - Persistencia de dados
  - Persistência de estado dos processos
  - Concorrência (paralelismo)
  - Lock (proteger um respetivo recuros)

## Descrição
A prova de conceito passa por um WebFrontEnd que faz pedidos a WebAPI por HTTP e gRPC para atualizar dados numa página Web.  
Extende-se também a requisições por HTTP, gRPC ou Pubsub para atualizar o estado de um serviço que chamados de Weighing.  
Uma vez que o envio a estrutura definida seja feito, atores são disparados, ações de persistência e mudança de estado são realizadas e retornos serão dados. Fora isso, toda modificação é propagada através de um broker pubsub.  
- gRPC:
```JSON
{
    "state": "",
    "weigh": 3500,
    "print": "XPTO"
}  
```  
 Manda-se através do header *dapr-app-id*, *caz-tenant* e *caz-kiosk*. Lembrando que é necessário que a aplicação cliente conheça e tenha compilado o ficheiro protos de comunicação em questão. 
   
- HTTP:
``` JSON
{
    "weigh": 22460,
    "tenant": "Cachapuz",
    "kiosk": "F_CI",
    "state": "XPTO",
    "lastWeighing": []
}
```  
- PUBSUB:
```JSON
{"data":{<payload_content>},"datacontenttype":"application/json"}
```  
É necessário respeitar esta estrutura de dados para comunicar-se através da interface pubsub Dapr. Sem isso a mensagem não chega aos serviços, e dependendo do QoS causa falhas por repetições indefinidas na tentativa de envio.
  
*Para maiores detalhes, informar-se através das documentações apontadas na base de conhecimento e bibliotecas postman contidas no projeto.*

# Implementação na Cloud Azure
Para implementarmos na Cloud Azure optamos pelo serviço Azure Container Apps Environment que dispõe da compatibilidade com os sidecars Dapr. Para isso é necessário ter conhecimento mínimo do uso das ferramentas Azure DevOps e dos contextos de uma infraestrutura em núvem. Todas as documentações relevantes para esta implementação também foram adicionadas na *Base de Conhecimento* existente neste mesmo documento.

## Azure Container Registry
Estes passos foram dados com base no contexto da prova de conceito desenvolvida, pode-se criar imagens únicas e subir imagens únicas sem a necessidade do Docker Compose, verificar em documentações relevantes.
- Instale Azure CLI e faça login
  ```console
  az login
  ```
- Faça login no ACR  
  ```console
  az acr login --name <nome-do-acr>
  ```
- Criar imagens (Docker Compose)
  ```console
  docker-compose build
  ```
- Envio das imagens que foram criadas (Docker Compose)
  ```console
  docker-compose push
  ```
 ## Azure Container App

 - gRPC container; provisionar container com protocolo HTTP2 (gRPC)
```powershell
az containerapp ingress enable --name <containerapp_name> --resource-group <resourcegroup_name> --target-port <port_target> --type external --transport http2
```
Exemplo:
```powershell
az containerapp ingress enable --name grpcserver --resource-group Dapr-Container-EUWest --target-port 80 --type external --transport http2
```
  