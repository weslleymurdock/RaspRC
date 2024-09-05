#!/bin/bash

sudo touch /etc/apache2/sites-available/rasp.local.conf
sudo echo "<VirtualHost *:80>" >> /etc/apache2/sites-available/rasp.local.conf
sudo echo "    ProxyPreserveHost On" >> /etc/apache2/sites-available/rasp.local.conf
sudo echo "    ProxyPass / http://127.0.0.1:5217/" >> /etc/apache2/sites-available/rasp.local.conf
sudo echo "    ProxyPassReverse / http://127.0.0.1:5217/" >> /etc/apache2/sites-available/rasp.local.conf
sudo echo "    ErrorLog ${APACHE_LOG_DIR}/rx-error.log" >> /etc/apache2/sites-available/rasp.local.conf
sudo echo "    CustomLog ${APACHE_LOG_DIR}/rx-access.log common" >> /etc/apache2/sites-available/rasp.local.conf
sudo echo "</VirtualHost>" >> /etc/apache2/sites-available/rasp.local.conf

mv -v $1/publish/* /var/www/rasprc-rx
