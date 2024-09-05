#!/bin/bash

sudo apt update 
sudo apt install apache2
sudo a2enmod proxy proxy_http
sudo systemctl restart apache2

sudo mkdir -p /var/www/rasprc-rx
sudo chmod 666 /var/www/rasprc-rx
