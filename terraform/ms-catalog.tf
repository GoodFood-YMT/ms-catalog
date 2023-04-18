# PostgreSQL
resource "azurerm_postgresql_server" "pgsql" {
  name                = "pgsql-catalog-${var.project_name}${var.environment_suffix}"
  location            = data.azurerm_resource_group.rg.location
  resource_group_name = data.azurerm_resource_group.rg.name

  administrator_login          = data.azurerm_key_vault_secret.db-login.value
  administrator_login_password = data.azurerm_key_vault_secret.db-password.value

  sku_name   = "GP_Gen5_4"
  version    = "11"
  storage_mb = 5120

  backup_retention_days        = 7
  geo_redundant_backup_enabled = false
  auto_grow_enabled            = true

  public_network_access_enabled    = true
  ssl_enforcement_enabled          = false
  ssl_minimal_tls_version_enforced = "TLSEnforcementDisabled"
}

resource "azurerm_postgresql_database" "db" {
  name                = "SampleDbDriver"
  resource_group_name = data.azurerm_resource_group.rg.name
  server_name         = azurerm_postgresql_server.pgsql.name
  charset             = "UTF8"
  collation           = "English_United States.1252"
}

resource "azurerm_postgresql_firewall_rule" "pgsql" {
  name                = "AllowAzureServices"
  resource_group_name = data.azurerm_resource_group.rg.name
  server_name         = azurerm_postgresql_server.pgsql.name
  start_ip_address    = "0.0.0.0"
  end_ip_address      = "0.0.0.0"
}

# # Webapp
# resource "azurerm_service_plan" "app-plan" {
#   name                = "plan-catalog-${var.project_name}${var.environment_suffix}"
#   resource_group_name = data.azurerm_resource_group.rg.name
#   location            = data.azurerm_resource_group.rg.location
#   os_type             = "Linux"
#   sku_name            = "S1"
# }

# resource "azurerm_linux_web_app" "webapp" {
#   name                = "web-catalog-${var.project_name}${var.environment_suffix}"
#   resource_group_name = data.azurerm_resource_group.rg.name
#   location            = data.azurerm_resource_group.rg.location
#   service_plan_id     = azurerm_service_plan.app-plan.id

#   site_config {

#     application_stack {
#       dotnet_version = "6.0"
#     }
#   }

#   connection_string {
#     name = "DefaultConnection"
#     type = "SQLAzure"
#     value = "Server=${azurerm_postgresql_server.pgsql.fqdn};Database=SampleDbDriver;Port=5432;User Id=${data.azurerm_key_vault_secret.db-login.value}@${azurerm_postgresql_server.pgsql.name};Password=${data.azurerm_key_vault_secret.db-password.value};"
#   }
# }

resource "azurerm_container_group" "webapp" {
  name                = "web-catalog-${var.project_name}${var.environment_suffix}"
  resource_group_name = data.azurerm_resource_group.rg.name
  location            = data.azurerm_resource_group.rg.location
  ip_address_type = "Public"
  dns_name_label = "aci-pgadmin-${var.project_name}${var.environment_suffix}"
  os_type = "Linux"

  container {
    name = "webapp"
    image = "YasinKara/ms-catalog:latest"
    cpu = "0.5"
    memory = "1.5"

    ports {
      port = 34856
      protocol = "TCP"
    }

    ports {
      port = 44351
      protocol = "TCP"
    }

    environment_variables = {
      "ConnectionStrings__DefaultConnection" = "Server=${azurerm_postgresql_server.pgsql.fqdn};Database=SampleDbDriver;Port=5432;User Id=${data.azurerm_key_vault_secret.db-login.value}@${azurerm_postgresql_server.pgsql.name};Password=${data.azurerm_key_vault_secret.db-password.value};"
    }
  }
}

