variable "environment_suffix" {
  type        = string
  default     = ""
  description = "Suffix to append to the environment name"
}

variable "location" {
  type        = string
  default     = "West Europe"
  description = "Location of the resources"
}

variable "project_name" {
  type        = string
  default     = "ms-catalog"
  description = "Name of the project"
}
