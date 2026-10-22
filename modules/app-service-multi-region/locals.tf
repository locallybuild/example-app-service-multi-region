locals {
  # Each region gets the same set of order resources: a Resource Group, a Storage
  # Account with an "orders" container, and a Service Bus namespace with an
  # "orders" queue.
  regions = {
    primary   = var.primary_location
    secondary = var.secondary_location
  }

  # Storage Account names are limited to 24 lowercase letters and digits, so they
  # drop the dashes and take the first three letters of the region.
  storage_prefix = replace(var.name_prefix, "-", "")

  # The application image, built from ../../app and pushed into the Locally
  # Container Registry by 5-container-registry.tf.
  image_name = "orders-api"

  # The image is tagged with a hash of the application directory, so any change
  # to the app produces a new tag: that re-triggers the build and push, and rolls
  # the App Service onto the freshly built image. Build artifacts are excluded
  # so they don't churn the hash.
  app_dir = "${path.module}/../../app"
  app_files = [
    for f in fileset(local.app_dir, "**") : f
    if !strcontains(f, "/bin/") && !strcontains(f, "/obj/")
  ]
  image_tag = substr(sha1(join(",", [for f in local.app_files : "${f}:${filesha1("${local.app_dir}/${f}")}"])), 0, 12)

  # The port the container listens on inside App Service (the app's PORT, exposed
  # to App Service as WEBSITES_PORT). App Service terminates TLS at the platform
  # and routes public traffic to the container on this port.
  app_port = 8080
}
