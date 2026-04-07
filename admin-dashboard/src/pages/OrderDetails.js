import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { useParams, useNavigate } from 'react-router-dom';

const API = 'http://localhost:5292';

export default function OrderDetails() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    axios.get(`${API}/api/orders/${id}`)
      .then(res => { setOrder(res.data); setLoading(false); })
      .catch(() => setLoading(false));
  }, [id]);

  if (loading) return <p>Loading...</p>;
  if (!order) return <p>Order not found.</p>;

  return (
    <div>
      <button onClick={() => navigate(-1)} style={{ marginBottom: '16px' }}>
        ← Back
      </button>

      <h2>📦 Order #{order.shortId}</h2>

      {/* Order Info */}
      <div style={{ 
        background: 'white', padding: '20px', 
        borderRadius: '8px', marginBottom: '16px' 
      }}>
        <h4>Order Information</h4>
        <p><strong>Full ID:</strong> {order.id}</p>
        <p><strong>Customer:</strong> {order.customerName}</p>
        <p><strong>Date:</strong> {new Date(order.createdAt).toLocaleString()}</p>
        <p><strong>Status:</strong> <span style={{
          padding: '4px 8px', borderRadius: '4px',
          background: order.status === 'Completed' ? '#2ecc71' :
                      order.status === 'Failed' ? '#e74c3c' : '#f39c12',
          color: 'white'
        }}>{order.status}</span></p>
        <p><strong>Total:</strong> €{order.totalAmount}</p>
        {order.failureReason && (
          <p style={{ color: 'red' }}>
            <strong>Failure Reason:</strong> {order.failureReason}
          </p>
        )}
      </div>

      {/* Items */}
      <div style={{ 
        background: 'white', padding: '20px', 
        borderRadius: '8px', marginBottom: '16px' 
      }}>
        <h4>Order Items</h4>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ background: '#f5f5f5' }}>
              <th style={{ padding: '8px', textAlign: 'left' }}>Product</th>
              <th style={{ padding: '8px' }}>Qty</th>
              <th style={{ padding: '8px' }}>Unit Price</th>
              <th style={{ padding: '8px' }}>Total</th>
            </tr>
          </thead>
          <tbody>
            {order.items?.map(item => (
              <tr key={item.id} style={{ borderBottom: '1px solid #eee' }}>
                <td style={{ padding: '8px' }}>{item.productName}</td>
                <td style={{ padding: '8px', textAlign: 'center' }}>
                  {item.quantity}
                </td>
                <td style={{ padding: '8px', textAlign: 'center' }}>
                  €{item.unitPrice}
                </td>
                <td style={{ padding: '8px', textAlign: 'center' }}>
                  €{item.totalPrice}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Payment Info */}
      {order.paymentRecord && (
        <div style={{ 
          background: 'white', padding: '20px', 
          borderRadius: '8px', marginBottom: '16px' 
        }}>
          <h4>💳 Payment Details</h4>
          <p><strong>Status:</strong> {order.paymentRecord.isApproved ? 
            '✅ Approved' : '❌ Rejected'}</p>
          {order.paymentRecord.transactionId && (
            <p><strong>Transaction ID:</strong> {order.paymentRecord.transactionId}</p>
          )}
          {order.paymentRecord.failureReason && (
            <p style={{ color: 'red' }}>
              <strong>Reason:</strong> {order.paymentRecord.failureReason}
            </p>
          )}
          <p><strong>Processed:</strong> {new Date(
            order.paymentRecord.processedAt).toLocaleString()}</p>
        </div>
      )}

      {/* Inventory Info */}
      {order.inventoryRecord && (
        <div style={{ 
          background: 'white', padding: '20px', 
          borderRadius: '8px', marginBottom: '16px' 
        }}>
          <h4>📦 Inventory Check</h4>
          <p><strong>Status:</strong> {order.inventoryRecord.isConfirmed ? 
            '✅ Confirmed' : '❌ Failed'}</p>
          {order.inventoryRecord.failureReason && (
            <p style={{ color: 'red' }}>
              <strong>Reason:</strong> {order.inventoryRecord.failureReason}
            </p>
          )}
        </div>
      )}

      {/* Shipping Info */}
      {order.shipmentRecord && (
        <div style={{ 
          background: 'white', padding: '20px', 
          borderRadius: '8px', marginBottom: '16px' 
        }}>
          <h4>🚚 Shipping Details</h4>
          <p><strong>Tracking:</strong> {order.shipmentRecord.trackingNumber}</p>
          <p><strong>Estimated Dispatch:</strong> {new Date(
            order.shipmentRecord.estimatedDispatch).toLocaleDateString()}</p>
        </div>
      )}
    </div>
  );
}