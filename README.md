# Hashed-Image-URL-Scanner

## Overview
A tool for searching for image files on a web server with unknown hash values in the urls.

Uses asynchronous tasks to check through multiple urls brute-force searching for a the correct hash codes and ids on a server until it gets a 200 code.

The specific information about the websites it's scanning are stripped out and can be passed in through the appsettings file.

## Possible Future Updates
I may or may not continue working on this depending on if I feel like it has practical uses.

The main issue is slowness with trying to find the correct 4-digit hexidecimal hash value which requires 65,536 in the worst case. In addition to needing to check this for multiple ids as the website I'm checking doesn't have clear patterns in how new uploads' ids increment.

I may make changes to try to decrease the number of checks or use multi-threading rather than Tasks to see if I can speed it up.

For example, I'm using a brute-force method to find these values but if I'm able to decrpyt them it would be more efficient.
