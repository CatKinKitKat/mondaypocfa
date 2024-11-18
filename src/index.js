const axios = require('axios');
const xml2js = require('xml2js');
const xmlbuilder = require('xmlbuilder');

module.exports = async function (context, myQueueItem) {
    context.log('Service Bus queue triggered with message:', myQueueItem);

    try {
        // Parse XML message to JSON
        const parser = new xml2js.Parser();
        const jsonMessage = await parser.parseStringPromise(myQueueItem);
        context.log('Parsed XML to JSON:', jsonMessage);

        // Validate XML here (basic validation for this example)
        // You can implement a custom XML schema validation logic if needed.
        if (!jsonMessage.HardwareStore || !jsonMessage.HardwareStore.Product) {
            throw new Error("Invalid XML structure. 'Product' element is missing.");
        }

        // Transform XML by adding StockStatus and LastUpdated fields
        jsonMessage.HardwareStore.Product.forEach(product => {
            const stock = parseInt(product.Stock[0], 10);
            product.StockStatus = stock > 0 ? 'In Stock' : 'Out of Stock';
            product.LastUpdated = new Date().toISOString();
        });

        context.log('Transformed JSON:', jsonMessage);

        // Convert the transformed JSON back to XML
        const builder = xmlbuilder.create(jsonMessage);
        const transformedXml = builder.end({ pretty: true });
        context.log('Transformed XML:', transformedXml);

        // Send the transformed XML to the REST API
        await sendToRestApi(transformedXml);

        context.log('Message processed successfully.');
    } catch (err) {
        context.log.error('Error processing the message:', err.message);
        throw err;
    }
};

// Function to send the transformed XML to a REST API (e.g., Pastebin or any other API)
async function sendToRestApi(transformedXml) {
    const apiUrl = process.env.RestApiUrl;  // Environment variable for API URL
    const apiKey = process.env.RestApiKey;  // Environment variable for API Key

    const headers = {
        'Content-Type': 'application/xml',
    };

    const data = {
        api_dev_key: apiKey,
        api_option: 'paste',
        api_paste_code: transformedXml,
    };

    try {
        const response = await axios.post(apiUrl, data, { headers });
        if (response.status === 200) {
            console.log('Successfully sent to REST API:', response.data);
        } else {
            console.log('Failed to send data to REST API:', response.status, response.data);
        }
    } catch (error) {
        console.error('Error sending to REST API:', error.message);
        throw error;
    }
}