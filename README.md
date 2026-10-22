```
ooooo                                      oooo  oooo              
`888'                                      `888  `888              
 888          .ooooo.   .ooooo.   .oooo.    888   888  oooo    ooo 
 888         d88' `88b d88' `"Y8 `P  )88b   888   888   `88.  .8'
 888         888   888 888        .oP"888   888   888    `88..8'
 888       o 888   888 888   .o8 d8(  888   888   888     `888'
o888ooooood8 `Y8bod8P' `Y8bod8P' `Y888""8o o888o o888o     .8'
                                                       .o..P'
                                                       `Y8P'
```

# Example: Take orders across two regions with App Service, Blob Storage and Service Bus within Locally

This example deploys a small orders API, written in .NET, to App Service on [Locally Build](https://locally.build). Each order is saved to Blob Storage and announced on a Service Bus queue. A Storage Account and a Service Bus namespace are deployed in two regions: London (primary) and Paris (secondary).

```
                         ┌───────────────────────────────────────────────────┐
                         │ ┌─ london (primary) ──────────────────────────┐   │
                         │ │  Storage Account     container "orders"     │   │
                         │ │  Service Bus         queue "orders"         │   │
 client ─▶ Orders API ──▶│ └─────────────────────────────────────────────┘   │
           (App Service) │ ┌─ paris (secondary) ─────────────────────────┐   │
                         │ │  Storage Account     container "orders"     │   │
                         │ │  Service Bus         queue "orders"         │   │
                         │ └─────────────────────────────────────────────┘   │
                         └───────────────────────────────────────────────────┘
```

The API lives in [`app/`](./app) and the end-to-end tests in [`tests/`](./tests). The API has these endpoints:

| Endpoint | What it does |
| --- | --- |
| `GET /products` | Lists the products you can order. The catalogue is fixed and held in memory. |
| `POST /orders` | Places an order: writes it as a JSON blob, then sends an `order-placed` message to the queue. |
| `GET /orders/{id}` | Reads an order back from Blob Storage. |
| `GET /healthz` | Liveness, used by App Service's health check. |
| `GET /_internal/healthcheck/{check}` | The health of one dependency: `storage-primary`, `storage-secondary`, `servicebus-primary` or `servicebus-secondary`. |

A request that's still waiting on Storage or Service Bus after 30 seconds gets a `504` back, rather than hanging for as long as the SDKs keep retrying.

## Requirements

* [Locally Build](https://locally.build). Using `locally chaos` to simulate an outage needs the Standard plan or above.
* Either [HashiCorp Terraform](https://terraform.io) or [OpenTofu](https://opentofu.org).
* Either [Docker](https://www.docker.com) or [Podman](https://podman.io) (recommended).
* The [.NET 10 SDK](https://dotnet.microsoft.com/download), to run the tests.
* The Locally Plugin for `Microsoft.Storage` installed (`locally plugin install --name Microsoft.Storage`).
* The Locally Plugin for `Microsoft.ServiceBus` installed (`locally plugin install --name Microsoft.ServiceBus`).
* The Locally Plugin for `Microsoft.Web` installed (`locally plugin install --name Microsoft.Web`).
* The Locally Plugin for `Microsoft.ContainerRegistry` installed (`locally plugin install --name Microsoft.ContainerRegistry`).

## Running the example

First, make sure your container runtime (Docker or Podman) is running, then start Locally:

```bash
locally build
```

With Locally running, open another terminal and initialise Terraform. This downloads the providers and sets up the module:

```bash
cd environments/locally
terraform init
```

> [!NOTE]
> You can use OpenTofu instead by running `tofu` wherever this says `terraform`.

Once Terraform is initialised, provision the example:

```bash
locally run terraform apply
```

During the apply, Terraform builds the API with `docker build` and pushes it to the Container Registry, so the `docker` command needs to be available. With Podman, the `podman-docker` shim or a `docker` alias works.

After you approve the plan and the resources are deployed, the API is running at the URL in the outputs:

```
api_url = "https://locally-example-orders-api.furnace.locally:5663"
```

Place an order:

```bash
curl -X POST https://locally-example-orders-api.furnace.locally:5663/orders \
  -H 'Content-Type: application/json' \
  -d '{"items":[{"productId":"espresso","quantity":2},{"productId":"croissant","quantity":1}]}'
```

Then peek at the message it sent:

```bash
locally servicebus queue peek --namespace locally-example-orders-london --name orders
```

## Running the tests

The end-to-end tests call the deployed API, so they need its URL. From the repository root:

```bash
export ORDERS_API_URL=$(terraform -chdir=environments/locally output -raw api_url)
dotnet test
```

Each request the tests make has 10 seconds to complete.

> [!TIP]
> Locally can simulate a region going down. Try taking London's Storage and Service Bus offline with `locally chaos`, then run the tests again, or ask your coding agent how the API behaves when its primary region is unavailable.
>
> ```bash
> locally chaos storage outage --percentage 100 --location london
> locally chaos servicebus outage --percentage 100 --location london
> ```
>
> To bring London back:
>
> ```bash
> locally chaos storage reset
> locally chaos servicebus reset
> ```

## Notes

The API reaches Storage and Service Bus with connection strings, passed in as App Service app settings. That keeps the example focused on the regions rather than on identity; a production app would use managed identities instead.

App Service's health check uses `/healthz`, which only says the API process is up. The `/_internal/healthcheck/*` endpoints check one dependency each, with a 5 second limit, so you can see what the API can reach without App Service taking its instance out of rotation when a dependency is unhealthy.

The API has no sign-in of its own: anyone who can reach it can place and read orders. A real API would sit behind authentication.

The API's logs are sent to a Log Analytics Workspace. You can query them in [the Monitoring section of the Locally Dashboard](https://localhost:5678/monitoring/components) by running:

```
AppServiceConsoleLogs | order by TimeGenerated desc
```

## Tearing it down

```bash
cd environments/locally
locally run terraform destroy
```

If you armed any chaos faults, reset them too:

```bash
locally chaos storage reset
locally chaos servicebus reset
```
