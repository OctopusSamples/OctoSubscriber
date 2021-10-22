terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 3.0"
    }
  }

  backend "s3" {
    bucket = "${var.backend_bucket}"
    key = "${var.backend_key}"
    region = "${var.backend_region}"
  }
}

provider "aws" {
    region  = "${var.region}"
}

resource "aws_sqs_queue" "terraform_queue" {
  name = "${var.queue_name}"
}