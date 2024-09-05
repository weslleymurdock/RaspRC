#!/bin/bash

sudo cp -v $1/rasp.local.service /etc/systemd/system/
sudo systemctl enable rasp.local.service
sudo systemctl start rasp.local.service
sudo touch /etc/apache2/sites-available/rasp.local.conf
sudo echo "<VirtualHost *:80>" > /etc/apache2/sites-available/rasp.local.conf
sudo echo "    ProxyPreserveHost On" >> /etc/apache2/sites-available/rasp.local.conf
sudo echo "    ProxyPass / http://127.0.0.1:5000/" >> /etc/apache2/sites-available/rasp.local.conf
sudo echo "    ProxyPassReverse / http://127.0.0.1:5000/" >> /etc/apache2/sites-available/rasp.local.conf
sudo echo "    ErrorLog ${APACHE_LOG_DIR}/rx-error.log" >> /etc/apache2/sites-available/rasp.local.conf
sudo echo "    CustomLog ${APACHE_LOG_DIR}/rx-access.log common" >> /etc/apache2/sites-available/rasp.local.conf
sudo echo "</VirtualHost>" >> /etc/apache2/sites-available/rasp.local.conf
sudo usermod -a -G www-data rx
sudo chown -R -f www-data:www-data /var/www/rasprc-rx
mv -v $1/publish/* /var/www/rasprc-rx 
cd /etc/apache2/sites-enabled/
sudo a2dissite * 
cd /etc/apache2/sites-available/
sudo a2ensite rasp.local.conf
sudo systemctl reload apache2
sudo chown rx -R /var/www/rasprc-rx/
